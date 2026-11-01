using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using System.Text.Json;

namespace Bookstore.WebClient.Service
{
    // Корзина, которая хранится в сессии пользователя.
    //
    // Используется в мок-режиме, а в режиме API — для гостей (до входа).
    // После входа гостевая корзина переносится в API (см. ApiCartService.MergeAsync).
    //
    // Сессия — небольшое хранилище на сервере, привязанное к браузеру через cookie.
    // Она хранит только строки и байты, поэтому список позиций
    // превращаем в JSON при записи и обратно при чтении.
    //
    // Методы асинхронные только ради общего интерфейса ICartService:
    // работа с сессией мгновенная, поэтому возвращаем готовые Task (Task.FromResult).
    public class SessionCartService : ICartService
    {
        private const string SessionKey = "Cart";

        private readonly IHttpContextAccessor _httpContextAccessor;

        // IHttpContextAccessor даёт доступ к текущему запросу (и его сессии)
        // из обычного класса, а не только из страницы.
        public SessionCartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public Task<List<CartItem>> GetItemsAsync(CancellationToken ct = default) => Task.FromResult(GetItems());

        public Task<string?> AddAsync(BookCard book, int quantity = 1, CancellationToken ct = default)
        {
            if (!book.InStock) return Task.FromResult<string?>($"«{book.Title}» зараз немає в наявності.");
            if (quantity < 1) return Task.FromResult<string?>(null);

            var items = GetItems();
            var item = items.FirstOrDefault(i => i.BookId == book.Id);

            if (item == null)
            {
                item = new CartItem
                {
                    BookId = book.Id,
                    Title = book.Title,
                    AuthorName = book.AuthorName,
                    ImageUrl = book.ImageUrl,
                    Price = book.Price
                };
                items.Add(item);
            }

            // Обновляем цену и остаток — они могли измениться с прошлого добавления.
            item.Price = book.Price;
            item.MaxQuantity = book.StockQuantity;

            // Нельзя положить больше, чем есть на складе: кладём сколько можно и сообщаем об этом
            var wanted = item.Quantity + quantity;
            item.Quantity = Math.Min(wanted, book.StockQuantity);
            Save(items);

            return Task.FromResult(wanted > book.StockQuantity
                ? $"«{book.Title}»: у наявності лише {book.StockQuantity} шт."
                : null);
        }

        public Task<string?> SetQuantityAsync(int bookId, int quantity, CancellationToken ct = default)
        {
            var items = GetItems();
            var item = items.FirstOrDefault(i => i.BookId == bookId);
            if (item == null) return Task.FromResult<string?>(null);

            string? error = null;
            if (quantity <= 0)
            {
                items.Remove(item);
            }
            else if (item.MaxQuantity > 0 && quantity > item.MaxQuantity)
            {
                item.Quantity = item.MaxQuantity;
                error = $"«{item.Title}»: у наявності лише {item.MaxQuantity} шт.";
            }
            else
            {
                item.Quantity = quantity;
            }

            Save(items);
            return Task.FromResult(error);
        }

        public Task RemoveAsync(int bookId, CancellationToken ct = default) => SetQuantityAsync(bookId, 0, ct);

        public Task ClearAsync(CancellationToken ct = default)
        {
            Session.Remove(SessionKey);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(CancellationToken ct = default) => Task.FromResult(GetItems().Sum(i => i.Quantity));

        private List<CartItem> GetItems()
        {
            var json = Session.GetString(SessionKey);
            return string.IsNullOrEmpty(json)
                ? []
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
        }

        private void Save(List<CartItem> items) =>
            Session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }
}
