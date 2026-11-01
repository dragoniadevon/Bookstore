using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Оформить заказ из корзины: POST /api/Order
    //
    // Книги и количества берутся из корзины пользователя на сервере,
    // клиент присылает только контакты и данные об оплате.
    // Оплату проводит платёжная система (в учебном проекте — эмуляция в клиенте),
    // поэтому сюда приходят только последние 4 цифры карты и номер транзакции —
    // полный номер карты API не получает никогда.
    public class CreateOrderDTO
    {
        [Required, MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        // Ровно 4 цифры
        [RegularExpression(@"^\d{4}$")]
        public string? CardLast4 { get; set; }

        [MaxLength(100)]
        public string? TransactionId { get; set; }
    }

    // Заказ: GET /api/Order, GET /api/Order/{number}
    public class OrderDTO
    {
        public string Number { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string? CardLast4 { get; set; }
        public string? TransactionId { get; set; }

        public decimal TotalAmount { get; set; }
        public List<OrderItemDTO> Items { get; set; } = [];
    }

    // Позиция заказа. UnitPrice — цена на момент покупки.
    public class OrderItemDTO
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
