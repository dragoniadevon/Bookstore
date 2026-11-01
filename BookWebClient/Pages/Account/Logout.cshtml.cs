using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages.Account
{
    // Выход: удаляем cookie входа. Корзина при этом остаётся (она в сессии).
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("/Index");

        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Ви вийшли з акаунта.";
            return RedirectToPage("/Index");
        }
    }
}
