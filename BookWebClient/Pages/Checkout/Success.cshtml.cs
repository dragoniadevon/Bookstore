using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BookWebClient.Pages.Checkout
{
    // "Спасибо за заказ": /Checkout/Success/BS-20261101-1234
    public class SuccessModel : PageModel
    {
        private readonly IOrderStore _orders;

        public SuccessModel(IOrderStore orders)
        {
            _orders = orders;
        }

        public Order Order { get; private set; } = null!;

        public IActionResult OnGet(string number)
        {
            // Ищем заказ только среди заказов текущего пользователя —
            // по чужому номеру ничего не покажем.
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var order = _orders.Find(email, number);
            if (order == null) return NotFound();

            Order = order;
            return Page();
        }
    }
}
