using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Publisher представляет издательство.
    //
    // Наследуется от BaseEntity, поэтому автоматически
    // получает Id, CreatedAt, ModifiedAt.
    public class Publisher : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        // Описание издательства 
        public string? Description { get; set; }

        // Книги издательства
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}