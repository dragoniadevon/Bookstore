using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Infrastructure.Model
{
    // Описание запроса, который нужно отправить в API.
    // Сервисы заполняют этот объект и передают его в ApiGateway.
    public class ApiRequest
    {
        // HTTP-метод. По умолчанию GET (получение данных).
        public ApiType ApiType { get; set; } = ApiType.GET;

        // Данные для тела запроса (для POST и PUT), например новый AuthorDto.
        // object? — подойдёт объект любого типа; ApiGateway сам превратит его в JSON.
        public object? Data { get; set; }

        // Путь к методу API, например "api/Book/5".
        // Начало адреса (https://localhost:7000/) берётся из настроек HttpClient.
        public string? URL { get; set; }

        // JWT-токен вошедшего пользователя. Понадобится, когда в API появится авторизация.
        public string? Token { get; set; }
    }
}
