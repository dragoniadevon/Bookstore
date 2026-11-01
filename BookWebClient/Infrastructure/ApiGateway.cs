using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Infrastructure.Model;
using System.Net;
using System.Text.Json;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Infrastructure
{
    // ApiGateway — "шлюз" к API: единственное место, где WebClient
    // реально отправляет HTTP-запросы.
    //
    // Зачем отдельный класс? Без него каждый сервис (AuthorService, BookService...)
    // сам создавал бы HttpClient, сам превращал данные в JSON, сам ловил ошибки.
    // Код повторялся бы много раз. Здесь это сделано один раз для всех.
    //
    // Схема:
    // Страница -> BookService -> ApiGateway -> HTTP -> Bookstore.Api
    public class ApiGateway : IApiGateWay
    {
        // Фабрика HttpClient. Через неё получаем клиент, настроенный в Program.cs
        // (с адресом API в BaseAddress).
        private readonly IHttpClientFactory _httpClientFactory;

        // Настройки JSON. JsonSerializerDefaults.Web — стандарт для веба:
        // имена полей в стиле camelCase ("stockQuantity") и без учёта регистра
        // при чтении. Именно так данные отдаёт ASP.NET Core API.
        //
        // static readonly — один объект на всё приложение. Создавать настройки
        // заново при каждом запросе дорого и бессмысленно.
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Доступ к текущему запросу браузера — чтобы взять JWT-токен вошедшего пользователя.
        // null — если шлюз создан вне веб-запроса (например, в тестах).
        private readonly IHttpContextAccessor? _httpContextAccessor;

        // Зависимости передаёт Dependency Injection (фабрика зарегистрирована через AddHttpClient).
        // "= null" — параметр необязательный: в тестах шлюз можно создать без HttpContext.
        public ApiGateway(IHttpClientFactory factory, IHttpContextAccessor? httpContextAccessor = null)
        {
            _httpClientFactory = factory;
            _httpContextAccessor = httpContextAccessor;
        }

        // Отправляет запрос в API и возвращает результат.
        //
        // <T> — тип, в который нужно превратить JSON-ответ.
        // Например, ApiRequest<List<BookDto>>(...) прочитает ответ как список книг.
        //
        // Метод никогда не бросает исключения наружу: любая ошибка превращается
        // в ApiResponse с IsSuccess = false. Странице не нужно писать try/catch,
        // достаточно проверить response.IsSuccess.
        //
        // CancellationToken ct — "сигнал отмены". Если пользователь закрыл
        // страницу, запрос к API тоже прервётся и не будет зря тратить ресурсы.
        public async Task<ApiResponse<T>> ApiRequest<T>(ApiRequest request, CancellationToken ct)
        {
            try
            {
                // Получаем HttpClient, настроенный в Program.cs.
                var client = _httpClientFactory.CreateClient(ApiClientName);

                // HttpRequestMessage — описание одного HTTP-запроса:
                // метод, адрес, заголовки и тело.
                HttpRequestMessage message = new HttpRequestMessage();

                // Сообщаем серверу, что ждём ответ в формате JSON.
                message.Headers.Add("Accept", "application/json");

                // Относительный путь ("api/Author") склеивается с BaseAddress клиента.
                message.RequestUri = new Uri(request.URL ?? string.Empty, UriKind.RelativeOrAbsolute);

                // Если есть данные (для POST и PUT), превращаем объект в JSON
                // и кладём в тело запроса.
                if (request.Data!= null)
                {
                    message.Content = JsonContent.Create(request.Data, options: JsonOptions);
                }

                // JWT-токен: либо передан явно в запросе, либо берём токен вошедшего
                // пользователя из его cookie (claim "access_token", см. SignInHelper).
                // Добавляем заголовок "Authorization: Bearer <токен>" — по нему API поймёт,
                // какой пользователь делает запрос. Заголовок ставим на конкретное сообщение,
                // а не на весь HttpClient, чтобы токен одного пользователя не попал к другому.
                var token = request.Token
                    ?? _httpContextAccessor?.HttpContext?.User.FindFirst(AccessTokenClaim)?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // Выбираем HTTP-метод по ApiType:
                // GET — получить, POST — создать, PUT — обновить, DELETE — удалить.
                switch (request.ApiType)
                {
                    case ApiType.POST:
                        message.Method = HttpMethod.Post;
                        break;

                    case ApiType.PUT:
                        message.Method = HttpMethod.Put;
                        break;

                    case ApiType.DELETE:
                        message.Method = HttpMethod.Delete;
                        break;

                    default:
                        message.Method = HttpMethod.Get;
                        break;
                }

                // Отправляем запрос и ждём ответа.
                HttpResponseMessage responseMessage = await client.SendAsync(message, ct);

                // IsSuccessStatusCode — true для кодов 200–299.
                // Иначе (400, 404, 500...) читаем текст ошибки от API и возвращаем неуспех.
                if (!responseMessage.IsSuccessStatusCode)
                {
                    // 401 — токена нет или он истёк: даём понятный текст вместо пустого ответа
                    if (responseMessage.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
                    {
                        return Fail<T>(responseMessage.StatusCode, "Сеанс завершився. Увійдіть знову.");
                    }

                    var errorBody = await responseMessage.Content.ReadAsStringAsync(ct);
                    return Fail<T>(responseMessage.StatusCode, ErrorText(errorBody));
                }

                // 204 или пустое тело (например, 201 Created() без данных) — успех без Result.
                // Если попытаться прочитать JSON из пустого ответа, будет ошибка.
                if (responseMessage.StatusCode == HttpStatusCode.NoContent
                    || responseMessage.Content.Headers.ContentLength == 0)
                {
                    return new ApiResponse<T> { HttpStatusCode = responseMessage.StatusCode, IsSuccess = true };
                }

                // Превращаем JSON из ответа в объект типа T (десериализация).
                var data = await responseMessage.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                return new ApiResponse<T> { HttpStatusCode = responseMessage.StatusCode, IsSuccess = true, Result = data };
            }
            // Ниже — обработка ошибок. Каждый catch ловит свой вид проблемы.
            catch (HttpRequestException ex) // нет сети, сервер недоступен
            {
                return Fail<T>(0, ex.Message);
            }
            // Запрос слишком долго ждал ответа (или был отменён).
            catch (TaskCanceledException)
            {
                return Fail<T>(HttpStatusCode.RequestTimeout, "Перевищено час очікування");
            }
            // API ответил, но JSON не удалось превратить в объект T
            // (например, поля не совпадают с DTO).
            catch (JsonException ex)
            {
                return Fail<T>(0, $"Не вдалося розібрати відповідь: {ex.Message}");
            }
        }


        // Текст ошибки из тела ответа.
        // API возвращает ошибки вроде Conflict("Неможливо видалити...") как JSON-строку —
        // в кавычках и с экранированными символами: "Не...". Превращаем её
        // в обычный текст. Если тело — не JSON-строка (например, объект с ошибками
        // валидации), оставляем как есть.
        private static string ErrorText(string body)
        {
            if (body.Length >= 2 && body[0] == '"')
            {
                try
                {
                    return JsonSerializer.Deserialize<string>(body) ?? body;
                }
                catch (JsonException)
                {
                    return body;
                }
            }

            return body;
        }

        // Вспомогательный метод: создаёт ответ с ошибкой.
        // "=> new() { ... }" — короткая запись метода, который сразу возвращает объект.
        private static ApiResponse<T> Fail<T>(HttpStatusCode statusCode, string? error) => new()
        {
            IsSuccess = false,
            HttpStatusCode = statusCode,
            ErrorMessage = error
        };
    }
}
