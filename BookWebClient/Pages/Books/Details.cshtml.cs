using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Books
{
    // Страница одной книги: /Books/Details/5
    public class DetailsModel : PageModel
    {
        private readonly ICatalogService _catalog;
        private readonly ICartService _cart;

        public DetailsModel(ICatalogService catalog, ICartService cart)
        {
            _catalog = catalog;
            _cart = cart;
        }

        public BookCard Book { get; private set; } = null!;

        // Другие книги того же автора — блок "Ещё у автора"
        public List<BookCard> SameAuthor { get; private set; } = [];

        // Сколько экземпляров этой книги уже лежит в корзине
        public int InCart { get; private set; }

        // id приходит из адреса (см. @page "{id:int}" в .cshtml)
        public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
        {
            var catalog = await _catalog.GetCatalogAsync(ct);
            var book = catalog.Books.FirstOrDefault(b => b.Id == id);

            // NotFound() — ответ 404 "страница не найдена"
            if (book == null) return NotFound();

            Book = book;
            SameAuthor = catalog.Books.Where(b => b.AuthorId == book.AuthorId && b.Id != book.Id).Take(4).ToList();
            InCart = (await _cart.GetItemsAsync(ct)).FirstOrDefault(i => i.BookId == id)?.Quantity ?? 0;
            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int id, int quantity, CancellationToken ct)
        {
            var book = await _catalog.GetBookAsync(id, ct);
            if (book == null) return NotFound();

            if (book.InStock)
            {
                var error = await _cart.AddAsync(book, Math.Max(1, quantity), ct);
                if (error == null)
                    TempData["Success"] = $"«{book.Title}» додано до кошика.";
                else
                    TempData["Error"] = error;
            }
            else
            {
                TempData["Error"] = $"«{book.Title}» зараз немає в наявності.";
            }

            return RedirectToPage(new { id });
        }
    }
}
