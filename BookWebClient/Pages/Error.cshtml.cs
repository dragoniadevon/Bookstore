using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BookWebClient.Pages
{
    // Код страницы ошибки (Error.cshtml).

    // Запрещаем браузеру и прокси кешировать страницу ошибки:
    // иначе пользователь мог бы увидеть старую ошибку, когда всё уже работает.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

    // Отключаем проверку антифорджери-токена (защита форм от подделки запросов).
    // Страница ошибки должна открываться всегда, даже если ошибка возникла
    // при отправке формы с неверным токеном.
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        // Уникальный номер запроса, в котором произошла ошибка
        public string? RequestId { get; set; }

        // Показывать ли RequestId на странице (только если он есть)
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public void OnGet()
        {
            // Берём Id текущей операции для трассировки, а если его нет —
            // внутренний номер запроса ASP.NET Core.
            // "?." — если Activity.Current равен null, не падаем, а получаем null.
            // "??" — если слева null, берём значение справа.
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }

}
