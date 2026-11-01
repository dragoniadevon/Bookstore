namespace Bookstore.WebClient.Models
{
    // Данные об издательстве для обмена с API.
    // Поля совпадают с PublisherDTO из Bookstore.Api.
    public class PublisherDto : IEntityDto
    {
        // Id издательства в базе
        public int Id { get; set; }

        // Название издательства
        public string Name { get; set; } = string.Empty;

        // Описание (может отсутствовать)
        public string? Description { get; set; }
    }
}
