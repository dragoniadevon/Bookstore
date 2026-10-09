using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // GenreDTO - объект для передачи данных о жанре между клиентом и API.
    // Entity Genre наружу не отдаём: используем DTO.
    public class GenreDTO
    {
        // ID жанра. При POST его генерирует база, при PUT берётся из адреса.
        public int Id { get; set; }

        // Название жанра. [Required] - обязательно, максимум 100 символов.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}