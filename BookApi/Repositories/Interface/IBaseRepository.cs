namespace Bookstore.Api.Repositories.Interface
{
    // Общий интерфейс для репозиториев.
    //
    // T — тип сущности, с которой будет работать конкретный репозиторий.
    // Например:
    // IRepository<Author> — репозиторий авторов
    // IRepository<Book>   — репозиторий книг
    //
    // where T : class означает, что T должен быть ссылочным типом.
    public interface IBaseRepository<T> where T : class
    {
        // Получить одну сущность по её Id.
        //
        // T? означает, что метод может вернуть null,
        // если сущность с таким Id не найдена.
        public Task<T?> GetByIdAsync(int id);

        // Получить все сущности данного типа.
        //
        // Например, для IRepository<Author>
        // метод вернёт список всех авторов.
        public Task<IEnumerable<T>> GetAllAsync();

        // Создать новую сущность в базе данных.
        //
        // Принимает объект T и возвращает созданный объект.
        // Например, Author для IRepository<Author>.
        public Task<T> CreateAsync(T obj);

        // Обновить существующую сущность.
        //
        // Может вернуть null, если сущность с таким Id
        // не существует в базе данных.
        public Task<T?> UpdateAsync(T obj);

        // Удалить сущность.
        //
        // Возвращает true, если сущность была найдена и удалена,
        // и false, если сущность не найдена.
        public Task<bool> DeleteAsync(T obj);
    }
}