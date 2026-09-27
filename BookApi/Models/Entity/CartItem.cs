using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
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