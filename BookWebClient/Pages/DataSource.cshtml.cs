using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages
{
    // Переключение источника данных (мок или API) для текущего посетителя.
    public class DataSourceModel : PageModel
    {
        private readonly IDataSourceSwitch _dataSource;
        private readonly SessionCartService _sessionCart;

        public DataSourceModel(IDataSourceSwitch dataSource, SessionCartService sessionCart)
        {
            _dataSource = dataSource;
            _sessionCart = sessionCart;
        }

        // Если открыть /DataSource в браузере напрямую — просто уходим на главную.
        public IActionResult OnGet() => RedirectToPage("/Index");

        // mode и returnUrl приходят из формы в шапке сайта.
        public async Task<IActionResult> OnPostAsync(DataSourceMode mode, string? returnUrl)
        {
            var changed = _dataSource.Mode != mode;
            _dataSource.SetMode(mode);

            var message = mode == DataSourceMode.Mock
                ? "Джерело даних: мок-дані (у пам'яті, без API)."
                : "Джерело даних: Bookstore.Api.";

            // В мок-режиме и в API разные пользователи, корзины и книги (разные Id).
            // Чтобы не смешивать их, при смене источника выходим из аккаунта
            // и очищаем корзину в сессии.
            if (changed)
            {
                await _sessionCart.ClearAsync();
                if (User.Identity?.IsAuthenticated == true)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    message += " Увійдіть знову.";
                }
            }

            TempData["Success"] = message;

            // Url.IsLocalUrl — защита от "открытого перенаправления": возвращаем
            // только на адреса нашего сайта, а не на чужой, подставленный в форму.
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
    }
}
