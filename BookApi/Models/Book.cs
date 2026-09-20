using Bookstore.Api.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models
{
    public class Book : BaseEntity
    {

        // Название книги
        public string Title { get; set; } = string.Empty;

        // Описание книги
        public string? Description { get; set; }

        // Цена книги
        public decimal Price { get; set; }

        // Книги, имеющиеся в наличии
        public int StockQuantity { get; set; }

        // URL изображения обложки книги
        public string? ImageUrl { get; set; }

        // Author ID
        public int AuthorId { get; set; }

        // Genre ID
        public int GenreId { get; set; }

        // Автор книги
        public Author Author { get; set; } = null!;

        // Жанр книги
        public Genre Genre { get; set; } = null!;

    }
}
