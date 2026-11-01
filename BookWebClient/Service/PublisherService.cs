using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service
{
    // Сервис для работы с издательствами через API (/api/Publisher).
    // Все методы унаследованы от BaseApiService (см. AuthorService).
    public class PublisherService : BaseApiService<PublisherDto>
    {
        public PublisherService(IApiGateWay gateway) : base(gateway, "api/Publisher")
        {
        }
    }
}
