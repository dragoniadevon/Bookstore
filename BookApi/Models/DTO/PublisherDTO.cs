using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // PublisherDTO — объект для передачи данных об издательстве между клиентом и API.
    // Entity Publisher наружу не отдаём: клиенту не нужны служебные поля и связи.
    public class PublisherDTO
    {
        // Id издательства. При POST его генерирует база, при PUT берётся из адреса.
        public int Id { get; set; }

        // [Required] — поле Name обязательно.
        // [MaxLength(100)] — максимум 100 символов.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Описание издательства (необязательное), максимум 500 символов
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
