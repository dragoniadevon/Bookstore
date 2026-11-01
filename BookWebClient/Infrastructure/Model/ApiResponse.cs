using System.Net;
namespace Bookstore.WebClient.Infrastructure.Model
{
    // Результат запроса к API.
    //
    // Вместо того чтобы бросать исключения, ApiGateway всегда возвращает этот объект.
    // Страница просто проверяет IsSuccess:
    //
    //     var response = await bookService.GetAllAsync();
    //     if (response.IsSuccess) { ...используем response.Result... }
    //     else { ...показываем response.ErrorMessage... }
    //
    // <T> — тип данных в ответе (BookDto, List<AuthorDto> и т.д.).
    public class ApiResponse<T>
    {
        // HTTP-код ответа: 200 OK, 404 Not Found, 500 и т.д.
        // 0 — если ответа вообще не было (API недоступен).
        public HttpStatusCode HttpStatusCode { get; set; }

        // true — запрос выполнен успешно
        public bool IsSuccess { get; set; }

        // Описание ошибки, если IsSuccess = false
        public string? ErrorMessage { get; set; }

        // Данные из ответа. null, если была ошибка или API ничего не вернул (204).
        public T? Result { get; set; }
    }
}
