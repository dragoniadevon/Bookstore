using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    public class PublisherDTO
    {
        public int Id { get; set; }

        // [Required] — поле Name обязательно.
        // [MaxLength(100)] — максимум 100 символов.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Описание издательства (необязательное)
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}