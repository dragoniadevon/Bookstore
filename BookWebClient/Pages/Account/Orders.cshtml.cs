using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BookWebClient.Pages.Account
{
    // История заказов: /Account/Orders (только для вошедших — см. Program.cs)
    public class OrdersModel : PageModel
    {
        private readonly IOrderStore _orders;

        public OrdersModel(IOrderStore orders)
        {
            _orders = orders;
        }

        public List<Order> Orders { get; private set; } = [];

        public void OnGet()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            Orders = _orders.GetByCustomer(email);
        }
    }
}
