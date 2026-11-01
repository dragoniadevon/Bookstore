using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    public class AuthorDTO
    {
        public int Id { get; set; }
        // [Required] указывает, что поле Name обязательно.
        // [MaxLength(100)] ограничивает максимальную длину имени
        // до 100 символов.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

    }
}