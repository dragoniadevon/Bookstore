using Bookstore.WebClient.Infrastructure.Model;

namespace Bookstore.WebClient.Service.Interface
{
    // Общий набор CRUD-запросов к API для одной сущности.
    //
    // CRUD — четыре базовые операции с данными:
    // Create (создать), Read (прочитать), Update (обновить), Delete (удалить).
    //
    // <T> — тип DTO: AuthorDto, BookDto и т.д.
    // Страница просит в конструкторе, например, IBaseApiService<BookDto>,
    // и Dependency Injection подставляет BookService (см. Program.cs).
    //
    // Все методы возвращают ApiResponse — внутри и данные, и признак успеха.
    // "CancellationToken ct = default" — параметр необязательный:
    // если его не передать, запрос просто нельзя будет отменить.
    public interface IBaseApiService<T> where T : class
    {
        // GET api/xxx — получить все записи
        Task<ApiResponse<List<T>>> GetAllAsync(CancellationToken ct = default);

        // GET api/xxx/{id} — получить одну запись
        Task<ApiResponse<T>> GetByIdAsync(int id, CancellationToken ct = default);

        // POST api/xxx — создать запись
        Task<ApiResponse<T>> CreateAsync(T entity, CancellationToken ct = default);

        // PUT api/xxx/{id} — обновить запись
        Task<ApiResponse<T>> UpdateAsync(int id, T entity, CancellationToken ct = default);

        // DELETE api/xxx/{id} — удалить запись.
        // object — потому что API на удаление ничего не возвращает (204 No Content),
        // важен только IsSuccess.
        Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
