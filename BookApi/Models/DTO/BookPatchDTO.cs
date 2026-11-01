using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Данные для частичного обновления книги (PATCH).
    //
    // Все поля nullable: null — "не меняем". Для чисел это важно:
    // раньше "не передано" определялось как 0, и поэтому нельзя было
    // выставить остаток 0 (книга закончилась). Теперь 0 — обычное значение,
    // а "не передано" — это null.
    public class BookPatchDTO
    {
        [MinLength(1)]
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public int? AuthorId { get; set; }
        public int? GenreId { get; set; }
        public int? PublisherId { get; set; }
    }
}
