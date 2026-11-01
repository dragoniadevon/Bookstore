using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BookWebClient.Pages.Account
{
    // Вход: /Account/Login
    public class LoginModel : PageModel
    {
        private readonly IUserStore _users;

        public LoginModel(IUserStore users)
        {
            _users = users;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        // Куда вернуть пользователя после входа. ASP.NET Core сам добавляет
        // ?ReturnUrl=... при перенаправлении со страницы, требующей входа.
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = _users.ValidateCredentials(Input.Email, Input.Password);
            if (user == null)
            {
                // Не уточняем, что именно неверно (email или пароль) —
                // иначе злоумышленник узнает, какие email зарегистрированы.
                ModelState.AddModelError(string.Empty, "Неправильний email або пароль.");
                return Page();
            }

            await SignInHelper.SignInAsync(HttpContext, user, Input.RememberMe);
            TempData["Success"] = $"Ласкаво просимо, {user.Name}!";
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
        }
    }

    public class LoginInput
    {
        [Required(ErrorMessage = "Вкажіть email")]
        [EmailAddress(ErrorMessage = "Неправильний формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запам'ятати мене")]
        public bool RememberMe { get; set; } = true;
    }
}
