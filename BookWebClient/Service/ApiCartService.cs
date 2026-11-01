using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Models.Api;
using Bookstore.WebClient.Service.Interface;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Service
{
    // Корзина вошедшего пользователя, которая хранится в Bookstore.Api (/api/Cart).
    //
    // Токен пользователя ApiGateway берёт сам — из cookie входа.
    // Остатки на складе проверяет API: если книг мало, он отвечает 409 с текстом,
    // и этот текст показывается пользователю.
    public class ApiCartService : ICartService
    {
        private const string Endpoint = "api/Cart";

        private readonly IApiGateWay _gateway;

        public ApiCartService(IApiGateWay gateway)
        {
            _gateway = gateway;
        }

        public async Task<List<CartItem>> GetItemsAsync(CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<ApiCartDto>(new ApiRequest { ApiType = ApiType.GET, URL = Endpoint }, ct);
            return response.IsSuccess && response.Result != null ? ToItems(response.Result) : [];
        }

        public async Task<string?> AddAsync(BookCard book, int quantity = 1, CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<ApiCartDto>(new ApiRequest
            {
                ApiType = ApiType.POST,
                URL = $"{Endpoint}/items",
                Data = new AddCartItemRequestDto { BookId = book.Id, Quantity = quantity }
            }, ct);

            return response.IsSuccess ? null : ErrorText(response);
        }

        public async Task<string?> SetQuantityAsync(int bookId, int quantity, CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<ApiCartDto>(new ApiRequest
            {
                ApiType = ApiType.PUT,
                URL = $"{Endpoint}/items/{bookId}",
                Data = new UpdateCartItemRequestDto { Quantity = Math.Max(0, quantity) }
            }, ct);

            return response.IsSuccess ? null : ErrorText(response);
        }

        public Task RemoveAsync(int bookId, CancellationToken ct = default) =>
            _gateway.ApiRequest<ApiCartDto>(new ApiRequest { ApiType = ApiType.DELETE, URL = $"{Endpoint}/items/{bookId}" }, ct);

        public Task ClearAsync(CancellationToken ct = default) =>
            _gateway.ApiRequest<object>(new ApiRequest { ApiType = ApiType.DELETE, URL = Endpoint }, ct);

        public async Task<int> CountAsync(CancellationToken ct = default) =>
            (await GetItemsAsync(ct)).Sum(i => i.Quantity);

        // Перенести гостевую корзину (из сессии) в корзину пользователя в API.
        // Вызывается сразу после входа. Токен передаём явно: cookie входа браузер
        // получит только в ответе на этот запрос, поэтому ApiGateway её ещё не видит.
        // Если какой-то книги не хватает на складе, пропускаем её — остальное переносим.
        public async Task MergeAsync(IEnumerable<CartItem> guestItems, string token, CancellationToken ct = default)
        {
            foreach (var item in guestItems)
            {
                await _gateway.ApiRequest<ApiCartDto>(new ApiRequest
                {
                    ApiType = ApiType.POST,
                    URL = $"{Endpoint}/items",
                    Data = new AddCartItemRequestDto { BookId = item.BookId, Quantity = item.Quantity },
                    Token = token
                }, ct);
            }
        }

        internal static List<CartItem> ToItems(ApiCartDto cart) => cart.Items.Select(i => new CartItem
        {
            BookId = i.BookId,
            Title = i.Title,
            AuthorName = i.AuthorName,
            ImageUrl = i.ImageUrl,
            Price = i.Price,
            Quantity = i.Quantity,
            MaxQuantity = i.StockQuantity
        }).ToList();

        // API присылает ошибку простым текстом; если текста нет — общее сообщение
        private static string ErrorText<T>(ApiResponse<T> response) =>
            string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? "Не вдалося оновити кошик. Спробуйте ще раз."
                : response.ErrorMessage;
    }
}
