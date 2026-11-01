using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // AuthorDTO — объект для передачи данных об авторе между клиентом и API.
    //
    // DTO (Data Transfer Object) — "объект для передачи данных".
    // Зачем он, если есть Entity Author?
    // - Entity — это таблица базы. В ней могут быть служебные поля
    //   (CreatedAt, ModifiedAt) и связи (Books), которые клиенту не нужны.
    // - DTO — это то, что API принимает и отдаёт наружу. Мы сами решаем,
    //   какие поля показать, и можем менять базу, не ломая клиентов.
    //
    // Преобразование Author <-> AuthorDTO делает AutoMapper (см. MappingConfig).
    public class AuthorDTO
    {
        // Id автора. При создании (POST) его генерирует база,
        // при обновлении (PUT) он берётся из адреса запроса.
        public int Id { get; set; }

        // [Required] указывает, что поле Name обязательно.
        // [MaxLength(100)] ограничивает максимальную длину имени
        // до 100 символов.
        // Если правило нарушено, [ApiController] сам вернёт клиенту 400 Bad Request.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

    }
}
