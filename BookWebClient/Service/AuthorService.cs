using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service
{
    // Сервис для работы с авторами через API (/api/Author).
    //
    // Все методы (GetAllAsync, CreateAsync...) унаследованы от BaseApiService.
    // Здесь мы только говорим, с каким DTO и по какому адресу работать.
    public class AuthorService : BaseApiService<AuthorDto>
    {
        // ": base(...)" — вызываем конструктор родительского класса
        // и передаём ему шлюз и адрес контроллера.
        public AuthorService(IApiGateWay gateway) : base(gateway, "api/Author")
        {
        }
    }
}
