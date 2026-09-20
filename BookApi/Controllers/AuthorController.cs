using Bookstore.Api.Models.DTO;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    [ApiController]
    [Route("api/Author")]
    public class AuthorController : Controller
    {
        IBaseService<AuthorDTO> _authorService;
        public AuthorController(IBaseService<AuthorDTO> _authorService)
        {
            this._authorService = _authorService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _authorService.GetAllAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _authorService.GetByIdAsync(id));
        }



    }
}
