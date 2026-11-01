using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Service
{
    // "Стрелочники" для корзины, входа и заказов — как SwitchableApiService для каталога.
    // Страницы получают ICartService / IAuthService / IOrderService и не знают,
    // работают они с API или с данными в памяти.

    // Корзина:
    // - режим API и пользователь вошёл (есть токен) -> корзина в API;
    // - иначе (мок-режим или гость) -> корзина в сессии.
    public class SwitchableCartService : ICartService
    {
        private readonly IDataSourceSwitch _switch;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApiCartService _api;
        private readonly SessionCartService _session;

        public SwitchableCartService(IDataSourceSwitch dataSourceSwitch, IHttpContextAccessor httpContextAccessor,
            ApiCartService api, SessionCartService session)
        {
            _switch = dataSourceSwitch;
            _httpContextAccessor = httpContextAccessor;
            _api = api;
            _session = session;
        }

        private ICartService Current =>
            _switch.Mode == DataSourceMode.Api && _httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaim) != null
                ? _api
                : _session;

        public Task<List<CartItem>> GetItemsAsync(CancellationToken ct = default) => Current.GetItemsAsync(ct);
        public Task<string?> AddAsync(BookCard book, int quantity = 1, CancellationToken ct = default) => Current.AddAsync(book, quantity, ct);
        public Task<string?> SetQuantityAsync(int bookId, int quantity, CancellationToken ct = default) => Current.SetQuantityAsync(bookId, quantity, ct);
        public Task RemoveAsync(int bookId, CancellationToken ct = default) => Current.RemoveAsync(bookId, ct);
        public Task ClearAsync(CancellationToken ct = default) => Current.ClearAsync(ct);
        public Task<int> CountAsync(CancellationToken ct = default) => Current.CountAsync(ct);
    }

    // Вход и регистрация: в режиме API — через /api/Auth, в мок-режиме — в памяти.
    public class SwitchableAuthService : IAuthService
    {
        private readonly IDataSourceSwitch _switch;
        private readonly ApiAuthService _api;
        private readonly MockAuthService _mock;

        public SwitchableAuthService(IDataSourceSwitch dataSourceSwitch, ApiAuthService api, MockAuthService mock)
        {
            _switch = dataSourceSwitch;
            _api = api;
            _mock = mock;
        }

        private IAuthService Current => _switch.Mode == DataSourceMode.Api ? _api : _mock;

        public Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default) =>
            Current.LoginAsync(email, password, ct);

        public Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default) =>
            Current.RegisterAsync(name, email, password, ct);
    }

    // Заказы: в режиме API — через /api/Order, в мок-режиме — в памяти.
    public class SwitchableOrderService : IOrderService
    {
        private readonly IDataSourceSwitch _switch;
        private readonly ApiOrderService _api;
        private readonly MockOrderService _mock;

        public SwitchableOrderService(IDataSourceSwitch dataSourceSwitch, ApiOrderService api, MockOrderService mock)
        {
            _switch = dataSourceSwitch;
            _api = api;
            _mock = mock;
        }

        private IOrderService Current => _switch.Mode == DataSourceMode.Api ? _api : _mock;

        public Task<OrderResult> PlaceOrderAsync(CheckoutData data, CancellationToken ct = default) => Current.PlaceOrderAsync(data, ct);
        public Task<List<Order>> GetMyOrdersAsync(CancellationToken ct = default) => Current.GetMyOrdersAsync(ct);
        public Task<Order?> GetAsync(string number, CancellationToken ct = default) => Current.GetAsync(number, ct);
    }
}
