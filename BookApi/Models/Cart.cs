using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    public class Cart : BaseEntity
    {
        public IEnumerable<Book>? Books { get; set; }

        // User ID
        public int UserId { get; set; }

        // Cart owner
        public User User { get; set; } = null!;

    }
}