using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service
{
    // Сервис для работы с книгами через API (/api/Book).
    // Все методы унаследованы от BaseApiService (см. AuthorService).
    public class BookService : BaseApiService<BookDto>
    {
        public BookService(IApiGateWay gateway) : base(gateway, "api/Book")
        {
        }
    }
}
