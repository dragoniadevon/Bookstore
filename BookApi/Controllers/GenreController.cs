using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // GenreController -> /api/Genre (регистр в адресе не важен,
    // /api/genre тоже работает).
    [ApiController]
    [Route("api/[controller]")]
    public class GenreController : ControllerBase
    {
        // Controller отвечает только за HTTP: вызывает Service
        // и выбирает код ответа.
        private readonly IBaseService<GenreDTO, GenrePatchDTO> _genreService;

        public GenreController(IBaseService<GenreDTO, GenrePatchDTO> genreService)
        {
            _genreService = genreService;
        }

        // GET /api/Genre - список всех жанров.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var genres = await _genreService.GetAllAsync();
            return Ok(genres);
        }

        // GET /api/Genre/{id} - один жанр или 404.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var genre = await _genreService.GetByIdAsync(id);
            if (genre == null)
            {
                return NotFound();
            }

            return Ok(genre);
        }

        // POST /api/Genre - создание жанра, 201 Created.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] GenreDTO newGenre)
        {
            var genre = await _genreService.CreateAsync(newGenre);
            if (genre == null)
            {
                return BadRequest();
            }

            return CreatedAtAction(nameof(GetById), new { id = genre.Id }, genre);
        }

        // PUT /api/Genre/{id} - полное обновление.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] GenreDTO genre)
        {
            // ID берём из URL
            genre.Id = id;

            var updated = await _genreService.UpdateAsync(genre);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        // PATCH /api/Genre/{id} - частичное обновление (поля GenrePatchDTO необязательные).
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(int id, [FromBody] GenrePatchDTO genre)
        {
            var updated = await _genreService.PatchAsync(id, genre);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        // DELETE /api/Genre/{id} - 204, 409 (у жанра есть книги) или 404.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _genreService.DeleteAsync(id);
            return result switch
            {
                DeleteResult.Deleted => NoContent(),
                DeleteResult.InUse => Conflict("Неможливо видалити жанр: у ньому є книги."),
                _ => NotFound()
            };
        }
    }
}