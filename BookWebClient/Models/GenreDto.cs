namespace Bookstore.WebClient.Models
{
    // Данные о жанре для обмена с API.
    // Поля совпадают с GenreDTO из Bookstore.Api.
    public class GenreDto : IEntityDto
    {
        // Id жанра в базе
        public int Id { get; set; }

        // Название жанра
        public string Name { get; set; } = string.Empty;
    }
}
