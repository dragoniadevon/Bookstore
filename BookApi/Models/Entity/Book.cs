using Bookstore.Api.Models.Entity.Base;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.Entity
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

        // Publisher ID
        public int PublisherId { get; set; }

        // Автор книги
        public Author Author { get; set; } = null!;

        // Жанр книги
        public Genre Genre { get; set; } = null!;

        // Издательство книги
        public Publisher Publisher { get; set; } = null!;
    }
}
