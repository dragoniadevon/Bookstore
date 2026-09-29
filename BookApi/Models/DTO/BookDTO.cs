using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.DTO
{
    // BookDTO (Data Transfer Object) - объект для передачи данных о книге
    // между клиентом (Swagger, браузер) и API.
    //
    // Зачем он нужен, если есть Entity Book:
    // - Entity Book описывает таблицу в базе данных и содержит служебные поля
    //   из BaseEntity и навигационные свойства (Author, Genre);
    // - DTO содержит только те данные, которые нужны клиенту,
    //   и позволяет задать правила проверки входящих данных.
    //
    // Наружу (в ответах API) мы отдаём DTO, а не Entity.
    // Преобразование Book <-> BookDTO выполняет AutoMapper (MappingConfig).
    public class BookDTO
    {
        // Идентификатор книги.
        //
        // При создании (POST) клиент его не задаёт: в BookService
        // Id обнуляется, и его генерирует база данных.
        // При PUT Id берётся из адреса (/api/Book/{id}).
        public int Id { get; set; }

        // Название книги.
        //
        // [Required] - поле обязательно, без названия запрос вернёт 400.
        // [MaxLength(200)] - не больше 200 символов.
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Описание книги.
        //
        // string? - поле необязательное, может быть null.
        // [MaxLength(2000)] - не больше 2000 символов.
        [MaxLength(2000)]
        public string? Description { get; set; }

        // Цена книги.
        //
        // [Range(0, double.MaxValue)] - цена не может быть отрицательной.
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        // Количество экземпляров в наличии.
        //
        // [Range(0, int.MaxValue)] - количество не может быть отрицательным.
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        // Ссылка на изображение обложки.
        //
        // Необязательное поле (string?), не длиннее 500 символов,
        // чтобы влезал длинный URL.
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // ID автора книги (внешний ключ на таблицу Authors).
        //
        // Мы передаём только ID, а не целый объект Author:
        // так запрос проще, и клиенту не нужно присылать данные автора.
        //
        // Существование автора проверяет BookService: если автора
        // с таким ID нет, книга не создаётся и API возвращает 400.
        public int AuthorId { get; set; }

        // ID жанра книги (внешний ключ на таблицу Genres).
        //
        // Работает так же, как AuthorId: BookService проверяет,
        // что такой жанр существует.
        public int GenreId { get; set; }
    }
}