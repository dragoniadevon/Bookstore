using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using System.Text.Json;

namespace Bookstore.WebClient.Service
{
    // Корзина, которая хранится в сессии пользователя.
    //
    // Сессия — небольшое хранилище на сервере, привязанное к браузеру через cookie.
    // Корзина работает и для гостей (без входа) и живёт, пока посетитель
    // активен на сайте (см. IdleTimeout в Program.cs).
    //
    // Сессия хранит только строки и байты, поэтому список позиций
    // превращаем в JSON при записи и обратно при чтении.
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

        public List<CartItem> GetItems()
        {
            var json = Session.GetString(SessionKey);
            return string.IsNullOrEmpty(json)
                ? []
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
        }

        public void Add(BookCard book, int quantity = 1)
        {
            if (!book.InStock || quantity < 1) return;

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

            // Math.Min — нельзя положить больше, чем есть на складе.
            item.Quantity = Math.Min(item.Quantity + quantity, book.StockQuantity);
            Save(items);
        }

        public void SetQuantity(int bookId, int quantity)
        {
            var items = GetItems();
            var item = items.FirstOrDefault(i => i.BookId == bookId);
            if (item == null) return;

            if (quantity <= 0)
            {
                items.Remove(item);
            }
            else
            {
                item.Quantity = item.MaxQuantity > 0 ? Math.Min(quantity, item.MaxQuantity) : quantity;
            }

            Save(items);
        }

        public void Remove(int bookId) => SetQuantity(bookId, 0);

        public void Clear() => Session.Remove(SessionKey);

        public int Count => GetItems().Sum(i => i.Quantity);

        public decimal Total => GetItems().Sum(i => i.Total);

        private void Save(List<CartItem> items) =>
            Session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }
}
