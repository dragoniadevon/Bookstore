using Bookstore.Api.Models.DTO;
using Bookstore.Api.Security;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // Регистрация и вход: /api/Auth
    //
    // Как работает авторизация через JWT:
    // 1. Клиент отправляет email и пароль на /api/Auth/login.
    // 2. API проверяет пароль и возвращает токен (подписанную строку с данными пользователя).
    // 3. Клиент добавляет токен ко всем следующим запросам:
    //        Authorization: Bearer eyJhbGciOi...
    // 4. На каждом запросе ASP.NET Core проверяет подпись и срок токена (UseAuthentication)
    //    и пускает в методы с [Authorize] только при правильном токене.
    // Сессий на сервере нет: всё нужное лежит в самом токене.
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        // POST /api/Auth/register — 200 + токен, 409 если email занят
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var result = await _auth.RegisterAsync(dto);
            return this.ToActionResult(result, response => Ok(response));
        }

        // POST /api/Auth/login — 200 + токен, 401 если email или пароль неверные
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var result = await _auth.LoginAsync(dto);
            return this.ToActionResult(result, response => Ok(response));
        }

        // GET /api/Auth/me — кто я. Без токена — 401.
        // [Authorize] — метод доступен только с правильным токеном.
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _auth.GetUserAsync(User.GetUserId());
            return user == null ? NotFound() : Ok(user);
        }
    }
}
