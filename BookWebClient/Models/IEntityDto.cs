namespace Bookstore.WebClient.Models
{
    // Общий признак всех DTO: у каждого есть Id.
    // Нужен мок-хранилищу, чтобы одним кодом искать, добавлять и удалять
    // и авторов, и книги, и жанры, и издательства.
    public interface IEntityDto
    {
        int Id { get; set; }
    }
}
