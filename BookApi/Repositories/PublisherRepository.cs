using Bookstore.Api.Data;
using Bookstore.Api.Models;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Repositories
{
    // Repository отвечает за работу с данными в базе.
    // Он не занимается HTTP, DTO и бизнес-логикой.
    //
    // Controller → Service → Repository → Database
    internal class PublisherRepository : IBaseRepository<Publisher>
    {
        // AppDbContext — основной объект EF Core для работы с БД.
        private readonly AppDbContext _dbContext;

        // DbSet<Publisher> соответствует таблице Publishers.
        private readonly DbSet<Publisher> _publishers;

        // AppDbContext передаётся через Dependency Injection.
        public PublisherRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _publishers = _dbContext.Publishers;
        }

        // CREATE
        // Создаёт новое издательство в базе данных.
        public async Task<Publisher> CreateAsync(Publisher obj)
        {
            await _publishers.AddAsync(obj);
            await _dbContext.SaveChangesAsync();
            return obj;
        }

        // DELETE
        // Удаляет издательство по Id.
        public async Task<DeleteResult> DeleteAsync(int id)
        {
            var publisher = await _publishers.FirstOrDefaultAsync(x => x.Id == id);

            if (publisher == null)
            {
                return DeleteResult.NotFound;
            }

            // Издательство с книгами удалять нельзя (ограничение Restrict в базе).
            if (await _dbContext.Books.AnyAsync(b => b.PublisherId == id))
            {
                return DeleteResult.InUse;
            }

            _publishers.Remove(publisher);
            await _dbContext.SaveChangesAsync();
            return DeleteResult.Deleted;
        }

        // GET ALL
        // Возвращает все издательства.
        public async Task<IEnumerable<Publisher>> GetAllAsync()
        {
            return await _publishers.ToListAsync();
        }

        // GET BY ID
        // Возвращает одно издательство по Id или null, если не найдено.
        public async Task<Publisher?> GetByIdAsync(int id)
        {
            return await _publishers.FirstOrDefaultAsync(x => x.Id == id);
        }

        // UPDATE
        // Обновляет существующее издательство.
        public async Task<Publisher?> UpdateAsync(Publisher obj)
        {
            var updateObj = await _publishers.FirstOrDefaultAsync(x => x.Id == obj.Id);

            // Если издательства с таким Id нет, обновлять нечего.
            if (updateObj == null)
            {
                return null;
            }

            // Если новое значение не передано, оставляем старое.
            updateObj.Name = obj.Name ?? updateObj.Name;
            updateObj.Description = obj.Description ?? updateObj.Description;

            // ModifiedAt проставит AppDbContext при сохранении.

            await _dbContext.SaveChangesAsync();

            return updateObj;
        }
    }
}