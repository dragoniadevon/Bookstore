using Bookstore.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // Превращает ServiceResult (ответ сервиса) в HTTP-ответ.
    // Одно место вместо одинакового switch в каждом методе контроллера.
    internal static class ServiceResultExtensions
    {
        // onSuccess — что вернуть при успехе (например, value => Ok(value)).
        // Ошибки возвращаются как текст с соответствующим кодом.
        public static IActionResult ToActionResult<T>(
            this ControllerBase controller,
            ServiceResult<T> result,
            Func<T, IActionResult> onSuccess)
        {
            return result.Status switch
            {
                ServiceStatus.Ok => onSuccess(result.Value!),
                ServiceStatus.NotFound => controller.NotFound(result.Error),
                ServiceStatus.BadRequest => controller.BadRequest(result.Error),
                ServiceStatus.Conflict => controller.Conflict(result.Error),
                ServiceStatus.Unauthorized => controller.Unauthorized(result.Error),
                _ => controller.StatusCode(500)
            };
        }
    }
}
