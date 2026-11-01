using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Cart
{
    // Корзина: /Cart
    // Доступна и гостям — войти нужно только при оформлении заказа.
    // Где хранится корзина (сессия или API), решает SwitchableCartService.
    public class IndexModel : PageModel
    {
        private readonly ICartService _cart;

        public IndexModel(ICartService cart)
        {
            _cart = cart;
        }

        public List<CartItem> Items { get; private set; } = [];
        public decimal Total { get; private set; }
        public int Count { get; private set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Items = await _cart.GetItemsAsync(ct);
            Total = Items.Sum(i => i.Total);
            Count = Items.Sum(i => i.Quantity);
        }

        // Кнопки "−" / "+" отправляют новое значение quantity
        public async Task<IActionResult> OnPostUpdateAsync(int bookId, int quantity, CancellationToken ct)
        {
            var error = await _cart.SetQuantityAsync(bookId, quantity, ct);
            if (error != null) TempData["Error"] = error;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int bookId, CancellationToken ct)
        {
            await _cart.RemoveAsync(bookId, ct);
            TempData["Success"] = "Книгу прибрано з кошика.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAsync(CancellationToken ct)
        {
            await _cart.ClearAsync(ct);
            TempData["Success"] = "Кошик очищено.";
            return RedirectToPage();
        }
    }
}
