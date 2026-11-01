using Bookstore.Api.Data;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Repositories
{
    // Repository отвечает за работу с данными в базе.
    //
    // В нашей архитектуре Repository не занимается:
    // - HTTP;
    // - DTO;
    // - бизнес-логикой;
    // - HTTP status codes.
    //
    // Его задача — выполнять операции с базой данных.
    //
    // Controller
    //     ↓
    // Service
    //     ↓
    // Repository
    //     ↓
    // Database
    internal class AuthorRepository : IBaseRepository<Author>
    {
        // AppDbContext — основной объект EF Core,
        // через который мы взаимодействуем с базой данных.
        private readonly AppDbContext _dbContext;

        // DbSet<Author> представляет набор сущностей Author,
        // с которым EF Core работает в базе данных.
        //
        // В нашей БД этому соответствует таблица Authors.
        private readonly DbSet<Author> _authors;


        // AppDbContext передаётся через Dependency Injection.
        public AuthorRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;

            // Получаем DbSet авторов из AppDbContext.
            //
            // Благодаря этому ниже можем писать _authors,
            // вместо _dbContext.Authors.
            _authors = _dbContext.Authors;
        }


        // CREATE
        //
        // Создаёт нового автора в базе данных.
        public async Task<Author> CreateAsync(Author obj)
        {
            // Добавляем Entity в DbSet.
            //
            // Пока что объект добавлен в Change Tracker EF Core,
            // но изменения ещё не обязательно отправлены в БД.
            await _authors.AddAsync(obj);

            // Сохраняем изменения в базе данных.
            //
            // Здесь EF Core сформирует SQL INSERT.
            await _dbContext.SaveChangesAsync();

            // Возвращаем созданную Entity.
            //
            // Например, если Id генерируется БД,
            // после SaveChangesAsync() здесь уже будет заполненный Id.
            return obj;
        }


        // DELETE
        //
        // Удаляет автора по Id.
        public async Task<bool> DeleteAsync(int id)
        {
            // Сначала ищем существующего автора.
            var author = await _authors.FirstOrDefaultAsync(x => x.Id == id);

            // Если автор найден — удаляем его.
            if (author != null)
            {
                // Помечаем Entity на удаление.
                _authors.Remove(author);

                // Сохраняем изменения.
                //
                // EF Core сформирует SQL DELETE.
                await _dbContext.SaveChangesAsync();

                return true;
            }

            // Если автора нет,
            // удалить его невозможно.
            return false;
        }


        // GET ALL
        //
        // Возвращает всех авторов.
        public async Task<IEnumerable<Author>> GetAllAsync()
        {
            // ToListAsync() выполняет запрос к БД
            // и возвращает список Entity.
            return await _authors.ToListAsync();
        }


        // GET BY ID
        //
        // Возвращает одного автора по Id.
        public async Task<Author?> GetByIdAsync(int id)
        {
            // FirstOrDefaultAsync() ищет первого автора,
            // у которого Id совпадает с переданным.
            //
            // Если автор найден → возвращается Author.
            // Если не найден → возвращается null.
            return await _authors.FirstOrDefaultAsync(x => x.Id == id);
        }


        // UPDATE
        //
        // Обновляет существующего автора.
        public async Task<Author?> UpdateAsync(Author obj)
        {
            // Сначала получаем существующего автора из БД.
            //
            // Важно:
            // EF Core начинает отслеживать полученную Entity.
            var updateObj = await _authors.FirstOrDefaultAsync(x => x.Id == obj.Id);

            // Если автора с таким Id нет,
            // обновлять нечего.
            if (updateObj == null)
            {
                return null;
            }

            // Изменяем свойства существующей Entity.
            //
            // Если новое имя не передано,
            // оставляем старое значение.
            updateObj.Name = obj.Name ?? updateObj.Name;

            // Обновляем дату изменения.
            updateObj.ModifiedAt = DateTime.UtcNow;

            // Здесь можно добавить остальные поля.
            //
            // Например:
            //
            // updateObj.SomeProperty = obj.SomeProperty ?? updateObj.SomeProperty;

            // EF Core отслеживает изменения updateObj.
            //
            // SaveChangesAsync() сформирует необходимый SQL UPDATE.
            await _dbContext.SaveChangesAsync();

            // Возвращаем обновлённую Entity.
            return updateObj;
        }
    }
}

