using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Корзина текущего пользователя: GET /api/Cart
    public class CartDTO
    {
        public List<CartItemDTO> Items { get; set; } = [];

        // Сколько всего книг (сумма количеств) и на какую сумму
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // Позиция корзины. Название, автор и цена — чтобы клиенту не нужно было
    // отдельно запрашивать каждую книгу.
    public class CartItemDTO
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        // Текущая цена книги (в заказе она "замораживается" в OrderItem.UnitPrice)
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        // Сколько книг есть на складе — больше положить нельзя
        public int StockQuantity { get; set; }

        public decimal Total { get; set; }
    }

    // Добавить книгу в корзину: POST /api/Cart/items
    public class AddCartItemDTO
    {
        [Range(1, int.MaxValue)]
        public int BookId { get; set; }

        // Сколько штук добавить (по умолчанию одну)
        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;
    }

    // Изменить количество: PUT /api/Cart/items/{bookId}. 0 — убрать из корзины.
    public class UpdateCartItemDTO
    {
        [Range(0, 1000)]
        public int Quantity { get; set; }
    }
}
