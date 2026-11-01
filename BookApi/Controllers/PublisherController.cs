using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // [ApiController] сообщает ASP.NET Core, что это API-контроллер:
    // он сам свяжет данные запроса с параметрами и проверит валидацию.
    [ApiController]

    // PublisherController -> /api/Publisher
    [Route("api/[controller]")]
    public class PublisherController : ControllerBase
    {
        // Controller не работает с базой напрямую, только через Service.
        private readonly IBaseService<PublisherDTO, PublisherPatchDTO> _publisherService;

        // Dependency Injection передаёт Service в Controller.
        public PublisherController(IBaseService<PublisherDTO, PublisherPatchDTO> publisherService)
        {
            _publisherService = publisherService;
        }

        // GET /api/Publisher
        // Получает список всех издательств.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var publisherList = await _publisherService.GetAllAsync();
            return Ok(publisherList);
        }

        // GET /api/Publisher/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var publisher = await _publisherService.GetByIdAsync(id);

            // Если Service не нашёл издательство, возвращаем 404.
            if (publisher == null)
            {
                return NotFound();
            }

            return Ok(publisher);
        }

        // POST /api/Publisher
        // Создаёт новое издательство.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] PublisherDTO newPublisher)
        {
            var publisher = await _publisherService.CreateAsync(newPublisher);

            if (publisher == null)
            {
                return BadRequest();
            }

            // 201 Created + созданный объект + адрес для GET.
            return CreatedAtAction(nameof(GetById), new { id = publisher.Id }, publisher);
        }

        // DELETE /api/Publisher/{id}
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _publisherService.DeleteAsync(id);

            // 204 — удалено, 409 — у издательства есть книги, 404 — не найдено.
            return result switch
            {
                DeleteResult.Deleted => NoContent(),
                DeleteResult.InUse => Conflict("Неможливо видалити видавництво: у нього є книги."),
                _ => NotFound()
            };
        }

        // PUT /api/Publisher/{id}
        // Полное обновление издательства.
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(
            int id,
            [FromBody] PublisherDTO publisher)
        {
            // ID берём из URL, а не из JSON.
            publisher.Id = id;

            var updatedPublisher = await _publisherService.UpdateAsync(publisher);

            // null значит, что издательство с таким ID не найдено.
            if (updatedPublisher == null)
            {
                return NotFound();
            }

            return Ok(updatedPublisher);
        }

        // PATCH /api/Publisher/{id}
        // Частичное обновление: меняем только переданные поля
        // (в PublisherPatchDTO все поля необязательные).
        [Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(
            int id,
            [FromBody] PublisherPatchDTO publisher)
        {
            var updatedPublisher = await _publisherService.PatchAsync(id, publisher);

            if (updatedPublisher == null)
            {
                return NotFound();
            }

            return Ok(updatedPublisher);
        }
    }
}