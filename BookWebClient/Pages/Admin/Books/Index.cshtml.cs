using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Admin.Books
{
    // Управление книгами: /Admin/Books (только роль Admin — см. Program.cs).
    // Работает в обоих режимах: в мок-режиме меняет данные в памяти, в API-режиме — в базе.
    public class IndexModel : PageModel
    {
        private readonly ICatalogService _catalog;
        private readonly IBaseApiService<BookDto> _books;
        private readonly IDataSourceSwitch _dataSource;

        public IndexModel(ICatalogService catalog, IBaseApiService<BookDto> books, IDataSourceSwitch dataSource)
        {
            _catalog = catalog;
            _books = books;
            _dataSource = dataSource;
        }

        public CatalogData Catalog { get; private set; } = new();
        public DataSourceMode Mode => _dataSource.Mode;

        public async Task OnGetAsync(CancellationToken ct)
        {
            Catalog = await _catalog.GetCatalogAsync(ct);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
        {
            var response = await _books.DeleteAsync(id, ct);
            if (response.IsSuccess)
                TempData["Success"] = "Книгу видалено.";
            else
                TempData["Error"] = $"Не вдалося видалити книгу: {response.ErrorMessage}";

            return RedirectToPage();
        }
    }
}
