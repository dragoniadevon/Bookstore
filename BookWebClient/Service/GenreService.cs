using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service
{
    // Сервис для работы с жанрами через API (/api/Genre).
    // Все методы унаследованы от BaseApiService (см. AuthorService).
    public class GenreService : BaseApiService<GenreDto>
    {
        public GenreService(IApiGateWay gateway) : base(gateway, "api/Genre")
        {
        }
    }
}
