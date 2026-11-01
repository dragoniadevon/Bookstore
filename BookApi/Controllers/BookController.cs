using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IBaseService<BookDTO, BookPatchDTO> _bookService;

        // Service передаёт Dependency Injection.
        public BookController(IBaseService<BookDTO, BookPatchDTO> bookService)
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
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] BookDTO newBook)
        {
            var book = await _bookService.CreateAsync(newBook);

            // null = автор, жанр или издательство с такими ID не найдены -> 400
            if (book == null)
            {
                return BadRequest("Автора, жанр або видавництво з указаним ID не знайдено.");
            }

            // 201 Created + созданный объект + адрес для GET
            return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
        }

        // PUT /api/Book/{id}
        // Полное обновление книги.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] BookDTO book)
        {
            // ID берём из URL, как в AuthorController
            book.Id = id;

            // null = книга (или автор/жанр/издательство) не найдена -> 404
            var updated = await _bookService.UpdateAsync(book);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        // PATCH /api/Book/{id}
        // Частичное обновление: меняются только переданные поля
        // (в BookPatchDTO все поля необязательные, null = не меняем).
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(int id, [FromBody] BookPatchDTO book)
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
        // Удаление книги: 204 No Content, 409 (книга есть в заказах) или 404.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _bookService.DeleteAsync(id);
            return result switch
            {
                DeleteResult.Deleted => NoContent(),
                DeleteResult.InUse => Conflict("Неможливо видалити книгу: вона є в оформлених замовленнях."),
                _ => NotFound()
            };
        }
    }
}