using Bookstore.WebClient.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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
        public static Task SignInAsync(HttpContext httpContext, ShopUser user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Email),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // IsPersistent = true — cookie переживёт закрытие браузера ("Запомнить меня")
            return httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = rememberMe });
        }
    }
}
