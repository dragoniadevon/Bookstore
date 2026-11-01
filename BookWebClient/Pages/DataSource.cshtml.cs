using Bookstore.WebClient.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWebClient.Pages
{
    // Переключение источника данных (мок или API) для текущего посетителя.
    public class DataSourceModel : PageModel
    {
        private readonly IDataSourceSwitch _dataSource;

        public DataSourceModel(IDataSourceSwitch dataSource)
        {
            _dataSource = dataSource;
        }

        // Если открыть /DataSource в браузере напрямую — просто уходим на главную.
        public IActionResult OnGet() => RedirectToPage("/Index");

        // mode и returnUrl приходят из формы в шапке сайта.
        public IActionResult OnPost(DataSourceMode mode, string? returnUrl)
        {
            _dataSource.SetMode(mode);

            TempData["Success"] = mode == DataSourceMode.Mock
                ? "Джерело даних: мок-дані (у пам'яті, без API)."
                : "Джерело даних: Bookstore.Api.";

            // Url.IsLocalUrl — защита от "открытого перенаправления": возвращаем
            // только на адреса нашего сайта, а не на чужой, подставленный в форму.
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
    }
}
