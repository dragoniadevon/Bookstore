using Bookstore.Api.Models.Entity.Base;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Book — книга в магазине.
    //
    // Entity (сущность) — это класс, который EF Core превращает в таблицу базы данных.
    // Каждое свойство — колонка таблицы Books, каждый объект Book — одна строка.
    //
    // Наследуется от BaseEntity, поэтому у книги уже есть Id, CreatedAt и ModifiedAt.
    public class Book : BaseEntity
    {

        // Название книги
        public string Title { get; set; } = string.Empty;

        // Описание книги.
        // string? (со знаком вопроса) — значение может отсутствовать (NULL в базе).
        public string? Description { get; set; }

        // Цена книги.
        // Для денег всегда используют decimal, а не double:
        // double хранит числа приблизительно (0.1 + 0.2 != 0.3), decimal — точно.
        public decimal Price { get; set; }

        // Сколько экземпляров книги есть в наличии на складе
        public int StockQuantity { get; set; }

        // URL изображения обложки книги
        public string? ImageUrl { get; set; }

        // ============================================================
        //   Связи с другими таблицами
        // ============================================================
        //
        // Для каждой связи есть ДВА свойства:
        //
        // 1. Внешний ключ (Foreign Key) — AuthorId, GenreId, PublisherId.
        //    Это обычное число, которое хранится в таблице Books
        //    и указывает на строку в другой таблице.
        //    Например, AuthorId = 3 означает "автор с Id = 3 в таблице Authors".
        //
        // 2. Навигационное свойство — Author, Genre, Publisher.
        //    Это сам связанный объект. В базе такой колонки нет:
        //    EF Core заполняет его сам по внешнему ключу, если попросить
        //    (например, через .Include(b => b.Author)).
        //
        // Сами связи и правила удаления настроены в AppDbContext.OnModelCreating.

        // Внешний ключ: Id автора книги
        public int AuthorId { get; set; }

        // Внешний ключ: Id жанра книги
        public int GenreId { get; set; }

        // Внешний ключ: Id издательства книги
        public int PublisherId { get; set; }

        // Навигационное свойство: автор книги.
        // "= null!" говорит компилятору: "не ругайся, что тут может быть null,
        // EF Core заполнит это свойство сам".
        public Author Author { get; set; } = null!;

        // Навигационное свойство: жанр книги
        public Genre Genre { get; set; } = null!;

        // Навигационное свойство: издательство книги
        public Publisher Publisher { get; set; } = null!;
    }
}
