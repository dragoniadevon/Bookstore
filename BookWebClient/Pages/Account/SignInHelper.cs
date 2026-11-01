using Bookstore.WebClient.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace BookWebClient.Pages.Account
{
    // Общий код входа для страниц Login и Register.
    public static class SignInHelper
    {
        // "Входит" пользователя: создаёт cookie с его данными.
        //
        // Claim (утверждение) — один факт о пользователе: имя, email, роль.
        // Набор claims упаковывается в ClaimsPrincipal и шифруется в cookie.
        // В следующих запросах ASP.NET Core расшифрует cookie, и на страницах
        // будут доступны User.Identity.Name, User.IsInRole("Admin") и т.д.
        //
        // В режиме API в cookie кладётся и JWT-токен (claim "access_token"):
        // ApiGateway достаёт его оттуда и добавляет к запросам в Bookstore.Api.
        // Cookie живёт столько же, сколько токен: когда токен истечёт,
        // пользователю нужно будет войти заново.
        public static Task SignInAsync(HttpContext httpContext, AuthResult auth, bool rememberMe)
        {
            var user = auth.User!;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Email),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            if (!string.IsNullOrEmpty(auth.Token))
            {
                claims.Add(new Claim(AccessTokenClaim, auth.Token));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // IsPersistent = true — cookie переживёт закрытие браузера ("Запомнить меня").
            // ExpiresUtc — когда cookie перестанет действовать (null — по настройкам из Program.cs).
            return httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = auth.ExpiresAt is DateTime expires ? new DateTimeOffset(DateTime.SpecifyKind(expires, DateTimeKind.Utc)) : null
                });
        }
    }
}
