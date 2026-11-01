using Bookstore.Api.Models.DTO;
using Bookstore.Api.Security;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // Заказы текущего пользователя: /api/Order (только с токеном)
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orders;

        public OrderController(IOrderService orders)
        {
            _orders = orders;
        }

        // GET /api/Order — мои заказы, новые сверху
        [HttpGet]
        public async Task<IActionResult> GetMyOrders() => Ok(await _orders.GetMyOrdersAsync(User.GetUserId()));

        // GET /api/Order/BS-20261102-483920 — один заказ (только свой, иначе 404)
        [HttpGet("{number}")]
        public async Task<IActionResult> GetByNumber(string number)
        {
            var order = await _orders.GetByNumberAsync(User.GetUserId(), number);
            return order == null ? NotFound() : Ok(order);
        }

        // POST /api/Order — оформить заказ из корзины.
        // 201 + заказ; 400 — корзина пуста; 409 — каких-то книг не хватает на складе.
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderDTO dto)
        {
            var result = await _orders.PlaceOrderAsync(User.GetUserId(), dto);
            return this.ToActionResult(result,
                order => CreatedAtAction(nameof(GetByNumber), new { number = order.Number }, order));
        }
    }
}
