using System.Security.Claims;

namespace Bookstore.Api.Security
{
    // Помощник для контроллеров: достать Id текущего пользователя из токена.
    public static class ClaimsPrincipalExtensions
    {
        // Метод расширения: в контроллере вызывается как User.GetUserId().
        // Работает только в методах с [Authorize] — там токен уже проверен.
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(AppClaims.UserId);
            return int.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException("В токене нет Id пользователя.");
        }
    }
}
