using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Данные для частичного обновления жанра (PATCH).
    // null — поле не меняем (подробнее см. AuthorPatchDTO).
    public class GenrePatchDTO
    {
        [MinLength(1)]
        [MaxLength(100)]
        public string? Name { get; set; }
    }
}
