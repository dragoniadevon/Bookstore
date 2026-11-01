using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages
{
    // Главная страница — каталог книг с поиском, фильтрами и сортировкой.
    //
    // PageModel — базовый класс для всех Razor-страниц.
    // Он даёт доступ к запросу (Request), пользователю (User),
    // методам перенаправления (RedirectToPage) и т.д.
    public class IndexModel : PageModel
    {
        private readonly ICatalogService _catalog;
        private readonly ICartService _cart;
        private readonly IDataSourceSwitch _dataSource;

        public IndexModel(ICatalogService catalog, ICartService cart, IDataSourceSwitch dataSource)
        {
            _catalog = catalog;
            _cart = cart;
            _dataSource = dataSource;
        }

        // ---------- Параметры фильтра из адресной строки ----------
        // [BindProperty(SupportsGet = true)] — заполнить свойство из адреса
        // при GET-запросе, например /?q=мастер&genre=1&sort=price.

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public int? Genre { get; set; }
        [BindProperty(SupportsGet = true)] public int? Author { get; set; }
        [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
        [BindProperty(SupportsGet = true)] public bool InStock { get; set; }

        // ---------- Данные для разметки ----------

        public CatalogData Catalog { get; private set; } = new();
        public List<BookCard> Books { get; private set; } = [];
        public bool IsMock => _dataSource.Mode == DataSourceMode.Mock;
        public bool HasFilters => !string.IsNullOrWhiteSpace(Q) || Genre != null || Author != null || InStock;

        // GET / — показать каталог.
        // CancellationToken ct ASP.NET Core передаёт сам: если пользователь
        // закрыл вкладку, запросы к API прервутся.
        public async Task OnGetAsync(CancellationToken ct)
        {
            Catalog = await _catalog.GetCatalogAsync(ct);

            // IEnumerable + цепочка Where — фильтры применяются по очереди,
            // только те, которые пользователь выбрал.
            IEnumerable<BookCard> books = Catalog.Books;

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                books = books.Where(b =>
                    b.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    b.AuthorName.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            if (Genre != null) books = books.Where(b => b.GenreId == Genre);
            if (Author != null) books = books.Where(b => b.AuthorId == Author);
            if (InStock) books = books.Where(b => b.InStock);

            // switch-выражение выбирает сортировку по значению Sort
            books = Sort switch
            {
                "price" => books.OrderBy(b => b.Price),
                "price_desc" => books.OrderByDescending(b => b.Price),
                "title" => books.OrderBy(b => b.Title),
                _ => books.OrderByDescending(b => b.InStock).ThenBy(b => b.Id)
            };

            Books = books.ToList();
        }

        // POST /?handler=AddToCart — кнопка "В корзину".
        // После действия делаем Redirect (паттерн Post/Redirect/Get): если пользователь
        // обновит страницу, браузер не отправит форму повторно и не добавит книгу второй раз.
        public async Task<IActionResult> OnPostAddToCartAsync(int bookId, string? returnUrl, CancellationToken ct)
        {
            var book = await _catalog.GetBookAsync(bookId, ct);

            if (book == null)
            {
                TempData["Error"] = "Книгу не знайдено.";
            }
            else if (!book.InStock)
            {
                TempData["Error"] = $"«{book.Title}» зараз немає в наявності.";
            }
            else
            {
                // AddAsync возвращает текст ошибки (например, "у наявності лише 2 шт.") или null
                var error = await _cart.AddAsync(book, 1, ct);
                if (error == null)
                    TempData["Success"] = $"«{book.Title}» додано до кошика.";
                else
                    TempData["Error"] = error;
            }

            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
    }
}
