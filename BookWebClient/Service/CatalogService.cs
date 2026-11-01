using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;

namespace Bookstore.WebClient.Service
{
    // Собирает витрину из четырёх запросов: книги, авторы, жанры, издательства.
    //
    // Откуда придут данные (мок или API), решает SwitchableApiService —
    // CatalogService об этом не знает и работает одинаково в обоих режимах.
    public class CatalogService : ICatalogService
    {
        private readonly IBaseApiService<BookDto> _books;
        private readonly IBaseApiService<AuthorDto> _authors;
        private readonly IBaseApiService<GenreDto> _genres;
        private readonly IBaseApiService<PublisherDto> _publishers;
        private readonly ILogger<CatalogService> _logger;

        public CatalogService(
            IBaseApiService<BookDto> books,
            IBaseApiService<AuthorDto> authors,
            IBaseApiService<GenreDto> genres,
            IBaseApiService<PublisherDto> publishers,
            ILogger<CatalogService> logger)
        {
            _books = books;
            _authors = authors;
            _genres = genres;
            _publishers = publishers;
            _logger = logger;
        }

        public async Task<CatalogData> GetCatalogAsync(CancellationToken ct = default)
        {
            // Запускаем все четыре запроса сразу и ждём, пока завершатся все.
            // Так быстрее, чем по очереди: общее время ≈ времени самого долгого запроса.
            var booksTask = _books.GetAllAsync(ct);
            var authorsTask = _authors.GetAllAsync(ct);
            var genresTask = _genres.GetAllAsync(ct);
            var publishersTask = _publishers.GetAllAsync(ct);
            await Task.WhenAll(booksTask, authorsTask, genresTask, publishersTask);

            var booksResponse = await booksTask;
            if (!booksResponse.IsSuccess)
            {
                _logger.LogWarning("Не удалось загрузить книги: {Error}", booksResponse.ErrorMessage);
                return new CatalogData { ErrorMessage = booksResponse.ErrorMessage ?? "Джерело даних недоступне" };
            }

            var authors = (await authorsTask).Result ?? [];
            var genres = (await genresTask).Result ?? [];
            var publishers = (await publishersTask).Result ?? [];

            return new CatalogData
            {
                Books = BuildCards(booksResponse.Result ?? [], authors, genres, publishers),
                Authors = authors.OrderBy(a => a.Name).ToList(),
                Genres = genres.OrderBy(g => g.Name).ToList(),
                Publishers = publishers.OrderBy(p => p.Name).ToList()
            };
        }

        public async Task<BookCard?> GetBookAsync(int id, CancellationToken ct = default)
        {
            // Каталог небольшой, поэтому проще взять его целиком: так сразу есть
            // имена автора, жанра и издательства.
            var catalog = await GetCatalogAsync(ct);
            return catalog.Books.FirstOrDefault(b => b.Id == id);
        }

        // Превращает BookDto в BookCard: подставляет имена вместо Id.
        private static List<BookCard> BuildCards(
            List<BookDto> books, List<AuthorDto> authors, List<GenreDto> genres, List<PublisherDto> publishers)
        {
            // Словари Id -> имя: поиск по ключу мгновенный, не нужно перебирать список.
            var authorNames = authors.ToDictionary(a => a.Id, a => a.Name);
            var genreNames = genres.ToDictionary(g => g.Id, g => g.Name);
            var publisherNames = publishers.ToDictionary(p => p.Id, p => p.Name);

            return books.Select(b => new BookCard
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                Price = b.Price,
                StockQuantity = b.StockQuantity,
                ImageUrl = b.ImageUrl,
                AuthorId = b.AuthorId,
                AuthorName = authorNames.GetValueOrDefault(b.AuthorId, "Невідомий автор"),
                GenreId = b.GenreId,
                GenreName = genreNames.GetValueOrDefault(b.GenreId, "Без жанру"),
                PublisherId = b.PublisherId,
                PublisherName = publisherNames.GetValueOrDefault(b.PublisherId, "—")
            }).ToList();
        }
    }
}
