using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Данные для частичного обновления издательства (PATCH).
    // null — поле не меняем (подробнее см. AuthorPatchDTO).
    public class PublisherPatchDTO
    {
        [MinLength(1)]
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
