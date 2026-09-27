using Bookstore.Api.Models.DTO;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers
{
    // [ApiController] сообщает ASP.NET Core, что этот класс является API-контроллером.
    // Благодаря этому ASP.NET Core автоматически выполняет часть стандартной работы:
    // - связывает данные из HTTP-запроса с параметрами метода;
    // - выполняет валидацию моделей;
    // - автоматически возвращает 400 Bad Request при некоторых ошибках валидации.
    [ApiController]

    // [Route("api/[controller]")] задаёт базовый адрес всех методов контроллера.
    //
    // [controller] автоматически заменяется на имя контроллера без слова "Controller".
    //
    // Поэтому:
    // AuthorController -> /api/Author
    //
    // Например:
    // GET /api/Author
    // GET /api/Author/5
    [Route("api/[controller]")]
    public class AuthorController : ControllerBase
    {
        // Controller не должен самостоятельно работать с базой данных.
        //
        // Вместо этого он обращается к Service, который содержит основную
        // логику работы с авторами.
        //
        // Controller отвечает в основном за HTTP:
        // - принимает HTTP-запрос;
        // - передаёт данные в Service;
        // - выбирает HTTP status code;
        // - возвращает результат клиенту.
        private readonly IBaseService<AuthorDTO> _authorService;

        // Dependency Injection передаёт необходимый Service в Controller.
        //
        // Мы не создаём Service через new AuthorService(...),
        // потому что ASP.NET Core сам создаёт и передаёт нужную реализацию.
        public AuthorController(IBaseService<AuthorDTO> authorService)
        {
            _authorService = authorService;
        }


        // GET /api/Author
        //
        // Метод получает список всех авторов.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Controller обращается к Service и получает данные.
            //
            // Controller не знает, как именно Service получил данные:
            // через Repository, Entity Framework, API и т.д.
            var authorList = await _authorService.GetAllAsync();

            // Ok() возвращает HTTP 200 OK.
            //
            // Объект authorList автоматически сериализуется
            // ASP.NET Core в JSON.
            //
            // Например:
            //
            // [
            //     {
            //         "id": 1,
            //         "name": "George Orwell"
            //     }
            // ]
            return Ok(authorList);
        }


        // GET /api/Author/{id}
        //
        // Например:
        // GET /api/Author/5
        //
        // {id} из Route связывается с параметром int id.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Получаем автора через Service.
            var author = await _authorService.GetByIdAsync(id);

            // Если Service не нашёл автора, он возвращает null.
            //
            // 404 Not Found означает:
            // "Запрос корректный, но такого ресурса не существует."
            if (author == null)
            {
                return NotFound();
            }

            // Если автор найден:
            // 200 OK + объект автора в JSON.
            return Ok(author);
        }


        // POST /api/Author
        //
        // POST обычно используется для создания нового ресурса.
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] AuthorDTO newAuthor)
        {
            // [FromBody] говорит ASP.NET Core взять JSON
            // из тела HTTP-запроса и преобразовать его в AuthorDTO.
            //
            // Например клиент отправляет:
            //
            // {
            //     "name": "George Orwell"
            // }
            //
            // ASP.NET Core создаёт объект AuthorDTO
            // и передаёт его в этот метод.
            var author = await _authorService.CreateAsync(newAuthor);

            // Если Service не смог создать автора,
            // возвращаем 400 Bad Request.
            //
            // В реальном API здесь можно отдельно обрабатывать
            // разные причины ошибки.
            if (author == null)
            {
                return BadRequest();
            }

            // При успешном создании ресурса правильный HTTP-код —
            // 201 Created.
            //
            // В более полноценном варианте здесь желательно использовать
            // CreatedAtAction(), чтобы вернуть созданный объект
            // и адрес, по которому его можно получить.
            return Created();
        }


        // DELETE /api/Author/{id}
        //
        // Например:
        // DELETE /api/Author/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Передаём ID автора в Service.
            //
            // Service возвращает true, если удаление прошло успешно.
            bool isAuthorDeleted = await _authorService.DeleteAsync(id);

            // Если удалить автора не удалось.
            //
            // В зависимости от реализации Service здесь может быть
            // более подходящим статус 404 Not Found, если автора
            // с таким ID вообще не существует.
            if (!isAuthorDeleted)
            {
                return BadRequest();
            }

            // 204 No Content означает:
            // операция успешно выполнена,
            // но возвращать данные клиенту не нужно.
            return NoContent();
        }


        // PUT /api/Author/{id}
        //
        // PUT обычно используется для полного обновления ресурса.
        //
        // Например:
        // PUT /api/Author/5
        //
        // {
        //     "name": "George Orwell"
        // }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(
            int id,
            [FromBody] AuthorDTO author)
        {
            // ID берём из URL, а не из JSON.
            //
            // Например:
            // PUT /api/Author/5
            //
            // Здесь id == 5.
            //
            // Это позволяет клиенту не передавать ID отдельно
            // внутри объекта запроса.
            author.Id = id;

            // Передаём объект в Service.
            //
            // Service самостоятельно решает:
            // - существует ли автор;
            // - как его обновить;
            // - как работать с Repository;
            // - как преобразовать Entity/DTO.
            var updatedAuthor = await _authorService.UpdateAsync(author);

            // Если Service вернул null,
            // значит автор с таким ID не найден.
            if (updatedAuthor == null)
            {
                return NotFound();
            }

            // Возвращаем обновлённый объект:
            // 200 OK + JSON.
            return Ok(updatedAuthor);
        }


        // PATCH /api/Author/{id}
        //
        // PATCH используется для частичного обновления ресурса.
        //
        // В отличие от PUT, здесь мы можем изменить только
        // отдельные поля объекта.
        //
        // Например:
        //
        // PATCH /api/Author/5
        //
        // {
        //     "name": "Eric Arthur Blair"
        // }
        //
        // Остальные свойства автора при этом не должны изменяться.
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(
            int id,
            [FromBody] AuthorDTO author)
        {
            // Передаём ID и данные в Service.
            //
            // Именно Service должен решить, какие поля нужно изменить.
            var updatedAuthor = await _authorService.PatchAsync(id, author);

            // Проверяем результат Service, а не исходный объект author.
            //
            // author — это данные, которые прислал клиент.
            // updatedAuthor — результат операции обновления.
            if (updatedAuthor == null)
            {
                return NotFound();
            }

            // Возвращаем уже обновлённый объект.
            return Ok(updatedAuthor);
        }
    }
}