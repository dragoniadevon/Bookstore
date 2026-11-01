using Bookstore.WebClient.Service;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BookWebClient.Pages.Account
{
    // Регистрация: /Account/Register
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _auth;
        private readonly CartMerger _cartMerger;

        public RegisterModel(IAuthService auth, CartMerger cartMerger)
        {
            _auth = auth;
            _cartMerger = cartMerger;
        }

        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid) return Page();

            var result = await _auth.RegisterAsync(Input.Name, Input.Email, Input.Password, ct);
            if (!result.Success)
            {
                // nameof(...) — имя поля без опечаток; ошибка выведется под полем Email
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Email)}", result.Error!);
                return Page();
            }

            // После регистрации сразу входим, чтобы не заставлять вводить данные ещё раз
            await SignInHelper.SignInAsync(HttpContext, result, rememberMe: true);
            await _cartMerger.MergeGuestCartAsync(result, ct);
            TempData["Success"] = $"Реєстрація пройшла успішно. Ласкаво просимо, {result.User!.Name}!";
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
        }
    }

    public class RegisterInput
    {
        [Required(ErrorMessage = "Вкажіть ім'я")]
        [StringLength(60, ErrorMessage = "Не довше 60 символів")]
        [Display(Name = "Ім'я")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть email")]
        [EmailAddress(ErrorMessage = "Неправильний формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Придумайте пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль — щонайменше 6 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        // [Compare] — значение должно совпадать с полем Password
        [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
        [DataType(DataType.Password)]
        [Display(Name = "Повторіть пароль")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
