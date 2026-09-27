using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    public class Genre : BaseEntity
    {
        // Genre name
        public string Name { get; set; } = string.Empty;

        // Книги этого жанра
        public IEnumerable<Book>? Books { get; set; }
    }
}