using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Checkout
{
    // "Спасибо за заказ": /Checkout/Success/BS-20261102-483920
    public class SuccessModel : PageModel
    {
        private readonly IOrderService _orders;

        public SuccessModel(IOrderService orders)
        {
            _orders = orders;
        }

        public Order Order { get; private set; } = null!;

        public async Task<IActionResult> OnGetAsync(string number, CancellationToken ct)
        {
            // Ищем заказ только среди заказов текущего пользователя —
            // по чужому номеру ничего не покажем (и в API, и в мок-режиме).
            var order = await _orders.GetAsync(number, ct);
            if (order == null) return NotFound();

            Order = order;
            return Page();
        }
    }
}
