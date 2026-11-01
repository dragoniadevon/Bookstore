using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Данные для частичного обновления автора (PATCH).
    //
    // Отличие от AuthorDTO: все поля необязательные (string?).
    // null означает "это поле не меняем". Поэтому клиент может прислать
    // только то, что хочет изменить, и не получит 400 из-за [Required].
    public class AuthorPatchDTO
    {
        // MinLength(1) — если имя передано, оно не может быть пустой строкой
        [MinLength(1)]
        [MaxLength(100)]
        public string? Name { get; set; }
    }
}
