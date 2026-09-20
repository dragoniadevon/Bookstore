using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    public class OrderItem : BaseEntity
    {
    
        // Order ID
        public int OrderId { get; set; }

        // Order
        public Order Order { get; set; } = null!;

        // Book ID
        public int BookId { get; set; }

        // Book
        public Book Book { get; set; } = null!;

        // Количество
        public int Quantity { get; set; }

        // Цена за единицу
        public decimal UnitPrice { get; set; }
    }
}