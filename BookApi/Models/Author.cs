using Bookstore.Api.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace BookApi.Entity
{
    // Сущность Author представляет автора книги.
    //
    // Наследуется от BaseEntity, поэтому Author автоматически
    // получает все свойства, которые определены в BaseEntity.
    //
    // Например, если в BaseEntity есть:
    // Id, CreatedAt, ModifiedAt
    // то Author также будет иметь эти свойства.
    public class Author : BaseEntity
    {
        // [Required] указывает, что поле Name обязательно.
        // EF Core создаст соответствующее ограничение в базе данных.
        //
        // [MaxLength(100)] ограничивает максимальную длину имени
        // до 100 символов.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}