using Bookstore.Api.Models.DTO;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // [ApiController]: включает автоматическую валидацию DTO
    // (например, [Required]) и привязку данных из запроса.
    [ApiController]

    // Базовый адрес контроллера: BookController -> /api/Book
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        // Controller отвечает только за HTTP: принимает запрос,
        // вызывает Service и выбирает код ответа.
        // С базой данных он напрямую не работает.
        private readonly IBaseService<BookDTO> _bookService;

        // Service передаёт Dependency Injection.
        public BookController(IBaseService<BookDTO> bookService)
        {
            _bookService = bookService;
        }

        // GET /api/Book
        // Список всех книг: 200 OK + JSON.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var books = await _bookService.GetAllAsync();
            return Ok(books);
        }

        // GET /api/Book/{id}
        // Одна книга по ID: 200 OK или 404, если не найдена.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var book = await _bookService.GetByIdAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            return Ok(book);
        }

        // POST /api/Book
        // Создание книги из JSON в теле запроса.
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] BookDTO newBook)
        {
            var book = await _bookService.CreateAsync(newBook);

            // null = автор или жанр с такими ID не найдены -> 400
            if (book == null)
            {
                return BadRequest("Автор или жанр с указанным ID не найден.");
            }

            // 201 Created + созданный объект + адрес для GET
            return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
        }

        // PUT /api/Book/{id}
        // Полное обновление книги.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] BookDTO book)
        {
            // ID берём из URL, как в AuthorController
            book.Id = id;

            // null = книга (или автор/жанр) не найдена -> 404
            var updated = await _bookService.UpdateAsync(book);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        // PATCH /api/Book/{id}
        // Частичное обновление: меняются только переданные поля.
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(int id, [FromBody] BookDTO book)
        {
            // null = книга (или автор/жанр) не найдена -> 404
            var updated = await _bookService.PatchAsync(id, book);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        // DELETE /api/Book/{id}
        // Удаление книги: 204 No Content или 404, если не найдена.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool isDeleted = await _bookService.DeleteAsync(id);
            if (!isDeleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}