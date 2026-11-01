using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Genre — жанр книги (роман, фантастика, детектив...).
    //
    // Связь с книгами "один ко многим": у жанра много книг,
    // а у каждой книги ровно один жанр (Book.GenreId).
    public class Genre : BaseEntity
    {
        // Название жанра
        public string Name { get; set; } = string.Empty;

        // Навигационное свойство: книги этого жанра.
        // В таблице Genres такой колонки нет — EF Core собирает список
        // по Book.GenreId, когда его об этом попросят (.Include(g => g.Books)).
        public IEnumerable<Book>? Books { get; set; }
    }
}
