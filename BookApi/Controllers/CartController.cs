using Bookstore.Api.Models.DTO;
using Bookstore.Api.Security;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // Корзина текущего пользователя: /api/Cart
    //
    // [Authorize] на всём контроллере — без токена все методы вернут 401.
    // Id пользователя берём из токена (User.GetUserId()), а не из запроса:
    // так нельзя подставить чужой Id и получить чужую корзину.
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cart;

        public CartController(ICartService cart)
        {
            _cart = cart;
        }

        // GET /api/Cart — содержимое корзины
        [HttpGet]
        public async Task<IActionResult> Get() => Ok(await _cart.GetAsync(User.GetUserId()));

        // POST /api/Cart/items — добавить книгу. 404 — нет книги, 409 — мало на складе
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemDTO dto)
        {
            var result = await _cart.AddItemAsync(User.GetUserId(), dto);
            return this.ToActionResult(result, cart => Ok(cart));
        }

        // PUT /api/Cart/items/5 — изменить количество (0 — убрать)
        [HttpPut("items/{bookId}")]
        public async Task<IActionResult> SetQuantity(int bookId, [FromBody] UpdateCartItemDTO dto)
        {
            var result = await _cart.SetQuantityAsync(User.GetUserId(), bookId, dto.Quantity);
            return this.ToActionResult(result, cart => Ok(cart));
        }

        // DELETE /api/Cart/items/5 — убрать книгу из корзины
        [HttpDelete("items/{bookId}")]
        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var result = await _cart.RemoveItemAsync(User.GetUserId(), bookId);
            return this.ToActionResult(result, cart => Ok(cart));
        }

        // DELETE /api/Cart — очистить корзину
        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            await _cart.ClearAsync(User.GetUserId());
            return NoContent();
        }
    }
}
