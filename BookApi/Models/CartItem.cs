using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    public class CartItem : BaseEntity
    {
        // Cart ID
        public int CartId { get; set; }

        // Cart
        public Cart Cart { get; set; } = null!;

        // Book ID
        public int BookId { get; set; }

        // Book
        public Book Book { get; set; } = null!;

        // Количество книг
        public int Quantity { get; set; }
    }
}