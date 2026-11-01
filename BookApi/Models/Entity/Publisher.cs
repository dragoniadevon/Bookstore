using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Publisher представляет издательство.
    //
    // Наследуется от BaseEntity, поэтому автоматически
    // получает Id, CreatedAt, ModifiedAt.
    public class Publisher : BaseEntity
    {
        // Название издательства
        public string Name { get; set; } = string.Empty;

        // Описание издательства (необязательное)
        public string? Description { get; set; }

        // Навигационное свойство: книги издательства.
        // Связь "один ко многим": у издательства много книг, у книги одно издательство.
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
