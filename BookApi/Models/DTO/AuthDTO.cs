using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // Данные для регистрации: POST /api/Auth/register
    public class RegisterDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        // Минимум 6 символов — как и в форме регистрации клиента
        [Required, MinLength(6), MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    // Данные для входа: POST /api/Auth/login
    public class LoginDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // Пользователь без секретных данных (без хеша пароля) — то, что можно отдать наружу
    public class UserDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // Ответ на вход и регистрацию: токен + когда он истекает + кто вошёл.
    //
    // Клиент сохраняет Token и передаёт его во всех следующих запросах
    // в заголовке "Authorization: Bearer <Token>".
    public class AuthResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDTO User { get; set; } = new();
    }
}
