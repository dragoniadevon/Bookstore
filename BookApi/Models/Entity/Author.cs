using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
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
        // Имя автора
        public string Name { get; set; } = string.Empty;

        // Биография (необязательная, поэтому string?)
        public string? Bio { get; set; }

        // Навигационное свойство: книги автора.
        // Связь "один ко многим": у автора много книг, у книги один автор (Book.AuthorId).
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
