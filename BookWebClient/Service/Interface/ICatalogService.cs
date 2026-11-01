using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Сервис витрины: собирает книги вместе с именами авторов, жанров и издательств.
    public interface ICatalogService
    {
        // Весь каталог и справочники для фильтров
        Task<CatalogData> GetCatalogAsync(CancellationToken ct = default);

        // Одна книга для витрины или null, если такой нет
        Task<BookCard?> GetBookAsync(int id, CancellationToken ct = default);
    }
}
