using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    // Сущность Author представляет автора книги.
    //
    // Наследуется от BaseEntity, поэтому Author автоматически
    // получает все свойства, которые определены в BaseEntity.
    //
    // Например, если в BaseEntity есть:
    // Id, CreatedAt, ModifiedAt
    // то Author также будет иметь эти свойства.
    public class Author : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        // Биография
        public string? Bio { get; set; }

        // Книги автора
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}