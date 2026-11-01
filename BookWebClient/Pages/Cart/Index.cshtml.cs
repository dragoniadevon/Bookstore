using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Cart
{
    // Корзина: /Cart
    // Доступна и гостям — войти нужно только при оформлении заказа.
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

        public void OnGet()
        {
            Items = _cart.GetItems();
            Total = Items.Sum(i => i.Total);
            Count = Items.Sum(i => i.Quantity);
        }

        // Кнопки "−" / "+" и поле количества отправляют новое значение quantity
        public IActionResult OnPostUpdate(int bookId, int quantity)
        {
            _cart.SetQuantity(bookId, quantity);
            return RedirectToPage();
        }

        public IActionResult OnPostRemove(int bookId)
        {
            _cart.Remove(bookId);
            TempData["Success"] = "Книгу прибрано з кошика.";
            return RedirectToPage();
        }

        public IActionResult OnPostClear()
        {
            _cart.Clear();
            TempData["Success"] = "Кошик очищено.";
            return RedirectToPage();
        }
    }
}
