using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BookWebClient.Pages.Admin.Books
{
    // Добавление и изменение книги:
    //   /Admin/Books/Edit     — новая книга
    //   /Admin/Books/Edit/5   — изменить книгу с Id = 5
    public class EditModel : PageModel
    {
        private readonly IBaseApiService<BookDto> _books;
        private readonly ICatalogService _catalog;

        public EditModel(IBaseApiService<BookDto> books, ICatalogService catalog)
        {
            _books = books;
            _catalog = catalog;
        }

        [BindProperty]
        public BookInput Input { get; set; } = new();

        // Пункты выпадающих списков (SelectListItem — текст + значение)
        public List<SelectListItem> Authors { get; private set; } = [];
        public List<SelectListItem> Genres { get; private set; } = [];
        public List<SelectListItem> Publishers { get; private set; } = [];

        public bool IsNew => Input.Id == 0;

        // id? — необязательный: без него создаём новую книгу
        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
        {
            if (id != null)
            {
                var response = await _books.GetByIdAsync(id.Value, ct);
                if (!response.IsSuccess || response.Result == null) return NotFound();
                Input = BookInput.FromDto(response.Result);
            }

            await LoadListsAsync(ct);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadListsAsync(ct);
                return Page();
            }

            var dto = Input.ToDto();
            var response = IsNew
                ? await _books.CreateAsync(dto, ct)
                : await _books.UpdateAsync(dto.Id, dto, ct);

            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, $"Не вдалося зберегти книгу: {response.ErrorMessage}");
                await LoadListsAsync(ct);
                return Page();
            }

            TempData["Success"] = IsNew ? $"Книгу «{dto.Title}» додано." : $"Книгу «{dto.Title}» збережено.";
            return RedirectToPage("Index");
        }

        private async Task LoadListsAsync(CancellationToken ct)
        {
            var catalog = await _catalog.GetCatalogAsync(ct);
            Authors = catalog.Authors.Select(a => new SelectListItem(a.Name, a.Id.ToString())).ToList();
            Genres = catalog.Genres.Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
            Publishers = catalog.Publishers.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        }
    }

    // Поля формы. Правила проверки совпадают с BookDTO в API.
    public class BookInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть назву")]
        [MaxLength(200)]
        [Display(Name = "Назва")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        [Display(Name = "Опис")]
        public string? Description { get; set; }

        [Range(0.01, 1_000_000, ErrorMessage = "Ціна має бути більшою за нуль")]
        [Display(Name = "Ціна, ₴")]
        public decimal Price { get; set; }

        [Range(0, 100_000, ErrorMessage = "Залишок не може бути від'ємним")]
        [Display(Name = "Залишок на складі")]
        public int StockQuantity { get; set; }

        [MaxLength(500)]
        [Url(ErrorMessage = "Вкажіть коректне посилання")]
        [Display(Name = "Посилання на обкладинку")]
        public string? ImageUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Оберіть автора")]
        [Display(Name = "Автор")]
        public int AuthorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Оберіть жанр")]
        [Display(Name = "Жанр")]
        public int GenreId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Оберіть видавництво")]
        [Display(Name = "Видавництво")]
        public int PublisherId { get; set; }

        public static BookInput FromDto(BookDto b) => new()
        {
            Id = b.Id, Title = b.Title, Description = b.Description, Price = b.Price,
            StockQuantity = b.StockQuantity, ImageUrl = b.ImageUrl,
            AuthorId = b.AuthorId, GenreId = b.GenreId, PublisherId = b.PublisherId
        };

        public BookDto ToDto() => new()
        {
            Id = Id, Title = Title.Trim(), Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Price = Price, StockQuantity = StockQuantity, ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim(),
            AuthorId = AuthorId, GenreId = GenreId, PublisherId = PublisherId
        };
    }
}
