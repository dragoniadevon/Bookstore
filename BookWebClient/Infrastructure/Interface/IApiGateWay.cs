using Bookstore.WebClient.Infrastructure.Model;

namespace Bookstore.WebClient.Infrastructure.Infrastracture
{
    // Интерфейс шлюза к API.
    //
    // Сервисы зависят от этого интерфейса, а не от класса ApiGateway напрямую.
    // Благодаря этому в тестах можно подставить "фальшивый" шлюз,
    // который не ходит в сеть, а сразу возвращает нужный ответ.
    public interface IApiGateWay
    {
        // Отправить запрос в API и получить ответ, превращённый в тип T.
        Task<ApiResponse<T>> ApiRequest<T> (ApiRequest request, CancellationToken cancellationToken);
    }
}
