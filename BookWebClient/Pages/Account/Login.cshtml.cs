using Bookstore.WebClient.Service;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BookWebClient.Pages.Account
{
    // Вход: /Account/Login
    public class LoginModel : PageModel
    {
        private readonly IAuthService _auth;
        private readonly CartMerger _cartMerger;

        public LoginModel(IAuthService auth, CartMerger cartMerger)
        {
            _auth = auth;
            _cartMerger = cartMerger;
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

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid) return Page();

            // В режиме API пароль проверяет Bookstore.Api и возвращает JWT-токен,
            // в мок-режиме — хранилище пользователей в памяти.
            var result = await _auth.LoginAsync(Input.Email, Input.Password, ct);
            if (!result.Success)
            {
                // Не уточняем, что именно неверно (email или пароль) —
                // иначе злоумышленник узнает, какие email зарегистрированы.
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }

            await SignInHelper.SignInAsync(HttpContext, result, Input.RememberMe);

            // Книги, которые гость положил в корзину до входа, переносим в его корзину в API
            await _cartMerger.MergeGuestCartAsync(result, ct);

            TempData["Success"] = $"Ласкаво просимо, {result.User!.Name}!";
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
