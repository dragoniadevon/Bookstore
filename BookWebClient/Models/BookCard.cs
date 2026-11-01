namespace Bookstore.WebClient.Models
{
    // Книга в том виде, в каком её показывает витрина магазина.
    //
    // API отдаёт BookDto только с Id автора, жанра и издательства (AuthorId = 3).
    // Покупателю же нужно видеть имена ("Михаил Булгаков"), поэтому CatalogService
    // собирает BookCard из книги и справочников: подставляет имена вместо Id.
    // ViewModel — модель, сделанная специально для отображения на странице.
    public class BookCard
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }

        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;

        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;

        public int PublisherId { get; set; }
        public string PublisherName { get; set; } = string.Empty;

        // Можно ли купить книгу — есть ли она на складе
        public bool InStock => StockQuantity > 0;
    }
}
