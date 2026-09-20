using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    public class Order : BaseEntity
    {
        // User ID
        public int UserId { get; set; }

        // User
        public User User { get; set; } = null!;

        // Общая сумма
        public decimal TotalAmount { get; set; }

        // Статус заказа
        public string Status { get; set; } = string.Empty;

        // Дата заказа
        public DateTime OrderDate { get; set; }


        // Заказать товары
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
