namespace Bookstore.WebClient.Models
{
    // Всё, что нужно витрине: книги и справочники для фильтров.
    public class CatalogData
    {
        public List<BookCard> Books { get; set; } = [];
        public List<AuthorDto> Authors { get; set; } = [];
        public List<GenreDto> Genres { get; set; } = [];
        public List<PublisherDto> Publishers { get; set; } = [];

        // Не удалось загрузить книги (например, API не запущен).
        // Страница показывает сообщение и предлагает переключиться на мок-данные.
        public bool HasError => ErrorMessage != null;

        // Текст ошибки от источника данных (если она была)
        public string? ErrorMessage { get; set; }
    }
}
