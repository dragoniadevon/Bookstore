using BookApi.Entity;
using Bookstore.Api.Data;
using Bookstore.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Repositories
{
    // Репозиторий отвечает за работу с сущностью Author в базе данных.
    // Здесь находятся операции создания, получения, изменения и удаления авторов.
    internal class AuthorRepository : IRepository<Author>
    {
        // AppDbContext — основной объект EF Core, через который мы взаимодействуем с БД.
        private readonly AppDbContext _dbContext;

        // DbSet<Author> представляет таблицу Authors в базе данных.
        // Через _authors мы выполняем запросы к этой таблице.
        private readonly DbSet<Author> _authors;

        public AuthorRepository(AppDbContext dbContext)
        {
            // Получаем AppDbContext через Dependency Injection.
            _dbContext = dbContext;

            // Получаем DbSet авторов из контекста,
            // чтобы не обращаться каждый раз к _dbContext.Authors.
            _authors = _dbContext.Authors;
        }

        // Создаёт нового автора в базе данных.
        public async Task<Author> CreateAsync(Author obj)
        {
            // Добавляем объект в DbSet.
            // На этом этапе запись ещё не обязательно сохранена в БД.
            await _authors.AddAsync(obj);

            // Сохраняем изменения в базе данных.
            await _dbContext.SaveChangesAsync();

            // Возвращаем созданного автора.
            // Например, здесь уже будет заполнен Id,
            // если он генерируется базой данных.
            return obj;
        }

        // Удаляет автора из базы данных.
        public async Task<bool> DeleteAsync(Author obj)
        {
            // Сначала ищем автора в базе по Id.
            var author = await _authors
                .FirstOrDefaultAsync(x => x.Id == obj.Id);

            // Если автор найден — удаляем его.
            if (author != null)
            {
                _authors.Remove(author);

                // Фактически выполняем DELETE в базе данных.
                await _dbContext.SaveChangesAsync();

                // Сообщаем вызывающему коду, что удаление прошло успешно.
                return true;
            }

            // Если автора с таким Id нет — удалять нечего.
            return false;
        }

        // Возвращает всех авторов из базы данных.
        public async Task<IEnumerable<Author>> GetAllAsync()
        {
            // ToListAsync() выполняет SQL-запрос и возвращает список авторов.
            return await _authors.ToListAsync();
        }

        // Возвращает автора по его Id.
        public async Task<Author?> GetByIdAsync(int id)
        {
            // Ищем первого автора, у которого Id совпадает
            // с Id, переданным в метод.
            var someAuthor = await _authors
                .FirstOrDefaultAsync(x => x.Id == id);

            // Если автор найден — возвращаем его.
            if (someAuthor != null)
            {
                return someAuthor;
            }

            // Если автор не найден — возвращаем null.
            return null;
        }

        // Обновляет данные существующего автора.
        public async Task<Author?> UpdateAsync(Author obj)
        {
            // Сначала находим существующего автора в базе.
            // Важно: здесь мы получаем объект, который отслеживается EF Core.
            var updateObj = await _authors
                .FirstOrDefaultAsync(x => x.Id == obj.Id);

            // Если автор найден — изменяем его данные.
            if (updateObj != null)
            {
                // Если новое имя не null — используем его.
                // Если null — оставляем старое значение.
                updateObj.Name = obj.Name ?? updateObj.Name;

                // Обновляем дату последнего изменения.
                updateObj.ModifiedAt = DateTime.UtcNow;

                // TODO:
                // Здесь можно добавить остальные свойства,
                // которые должны обновляться.
                //
                // Например:
                // updateObj.SomeProperty = obj.SomeProperty ?? updateObj.SomeProperty;

                // EF Core обнаружит изменения объекта
                // и сохранит их в базе данных.
                await _dbContext.SaveChangesAsync();

                // Возвращаем обновлённый объект.
                return updateObj;
            }

            // Если автора с таким Id нет — возвращаем null.
            return null;
        }
    }
}