using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Account
{
    // История заказов: /Account/Orders (только для вошедших — см. Program.cs)
    public class OrdersModel : PageModel
    {
        private readonly IOrderService _orders;

        public OrdersModel(IOrderService orders)
        {
            _orders = orders;
        }

        public List<Order> Orders { get; private set; } = [];

        public async Task OnGetAsync(CancellationToken ct)
        {
            Orders = await _orders.GetMyOrdersAsync(ct);
        }
    }
}
