namespace Bookstore.WebClient.Models
{
    // Данные о книге для обмена с API.
    // Поля совпадают с BookDTO из Bookstore.Api.
    public class BookDto : IEntityDto
    {
        // Id книги в базе
        public int Id { get; set; }

        // Название
        public string Title { get; set; } = string.Empty;

        // Описание (может отсутствовать)
        public string? Description { get; set; }

        // Цена. decimal — точный тип для денег.
        public decimal Price { get; set; }

        // Сколько экземпляров есть на складе
        public int StockQuantity { get; set; }

        // Ссылка на картинку обложки
        public string? ImageUrl { get; set; }

        // Id автора, жанра и издательства.
        // Сами названия здесь не приходят — чтобы их показать,
        // нужно отдельно получить автора/жанр/издательство по Id.
        public int AuthorId { get; set; }
        public int GenreId { get; set; }
        public int PublisherId { get; set; }
    }
}
