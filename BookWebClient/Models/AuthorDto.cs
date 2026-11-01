namespace Bookstore.WebClient.Models
{
    // Данные об авторе, которые WebClient получает от API и отправляет в него.
    //
    // Это "зеркало" AuthorDTO из Bookstore.Api: имена и типы полей должны совпадать,
    // иначе JSON из ответа не превратится в объект (поле просто останется пустым).
    // Проекты не ссылаются друг на друга, поэтому класс описан здесь ещё раз.
    public class AuthorDto : IEntityDto
    {
        // Id автора в базе
        public int Id { get; set; }

        // Имя автора
        public string Name { get; set; } = string.Empty;
    }
}
