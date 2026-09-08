using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    public class AuthorController : Controller
    {
        public IActionResult Index()
        {
            return Ok();
        }
    }
}
