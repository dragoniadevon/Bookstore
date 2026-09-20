using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    public class Genre : BaseEntity
    {
        // Genre name
        public string Name { get; set; } = string.Empty;

        // Книги этого жанра
        public IEnumerable<Book>? Books { get; set; }
    }
}