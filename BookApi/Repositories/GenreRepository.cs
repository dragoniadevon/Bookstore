using Bookstore.Api.Data;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Repositories
{
    // Repository работает с базой данных для сущности Genre.
    internal class GenreRepository : IBaseRepository<Genre>
    {
        // Контекст EF Core для доступа к таблице Genres.
        private readonly AppDbContext _context;

        // Контекст передаётся через Dependency Injection.
        public GenreRepository(AppDbContext context)
        {
            _context = context;
        }

        // Получить все жанры (только чтение, без отслеживания).
        public async Task<IEnumerable<Genre>> GetAllAsync()
        {
            return await _context.Genres.AsNoTracking().ToListAsync();
        }

        // Получить жанр по ID или null, если такого нет.
        public async Task<Genre?> GetByIdAsync(int id)
        {
            return await _context.Genres.FirstOrDefaultAsync(g => g.Id == id);
        }

        // Создать жанр: INSERT, Id заполняет база.
        public async Task<Genre> CreateAsync(Genre entity)
        {
            _context.Genres.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // Обновить жанр: UPDATE.
        public async Task<Genre?> UpdateAsync(Genre entity)
        {
            _context.Genres.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // Удалить жанр по ID. false, если жанра нет.
        public async Task<bool> DeleteAsync(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return false;
            }

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}