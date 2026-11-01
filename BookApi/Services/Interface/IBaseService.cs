using Bookstore.Api.Models;

namespace Bookstore.Api.Services.Interface
{
    // Общий интерфейс для всех сервисов API.
    //
    // Интерфейс — это "договор": он перечисляет, какие методы должны быть
    // у класса, но не говорит, как они устроены внутри.
    // AuthorService, BookService, GenreService и PublisherService реализуют
    // этот интерфейс, поэтому у всех одинаковый набор методов.
    //
    // <T, TPatch> — обобщённые (generic) параметры:
    // - T      — обычный DTO (AuthorDTO), используется для чтения, POST и PUT;
    // - TPatch — DTO для PATCH (AuthorPatchDTO), в нём все поля необязательные.
    // Например, IBaseService<AuthorDTO, AuthorPatchDTO> — сервис авторов.
    // "where T : class" — T может быть только классом (не int, не struct).
    //
    // Зачем интерфейс, если можно сразу использовать класс?
    // Controller зависит от интерфейса, а не от конкретного класса.
    // Значит, реализацию можно подменить (например, на тестовую),
    // не меняя код Controller. Какой класс подставить, решает
    // регистрация в Program.cs.
    //
    // Task<...> означает, что метод асинхронный: его вызывают через await,
    // и пока идёт запрос к базе, поток не простаивает, а обслуживает другие запросы.
    public interface IBaseService<T, TPatch>
        where T : class
        where TPatch : class
    {
        // Получить одну запись по Id. T? — вернёт null, если не нашлось.
        public Task<T?> GetByIdAsync(int id);

        // Получить все записи.
        public Task<IEnumerable<T>> GetAllAsync();

        // Создать новую запись. Возвращает созданный объект (уже с Id из базы).
        public Task<T?> CreateAsync(T entity);

        // Полное обновление (PUT): заменяет все поля записи.
        public Task<T?> UpdateAsync(T entity);

        // Частичное обновление (PATCH): меняет только переданные (не null) поля.
        public Task<T?> PatchAsync(int id, TPatch patch);

        // Удалить запись: Deleted, NotFound или InUse (см. DeleteResult).
        public Task<DeleteResult> DeleteAsync(int id);
    }
}
