using Bookstore.Api.Data;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Repositories
{
    // Repository работает с базой данных для сущности Book.
    // Он знает только про Entity, а DTO и HTTP-коды его не касаются.
    internal class BookRepository : IBaseRepository<Book>
    {
        // Контекст EF Core для доступа к таблице Books.
        private readonly AppDbContext _context;

        // Контекст передаётся через Dependency Injection.
        public BookRepository(AppDbContext context)
        {
            _context = context;
        }

        // Получить все книги.
        // AsNoTracking: только чтение, EF Core не отслеживает объекты.
        public async Task<IEnumerable<Book>> GetAllAsync()
        {
            return await _context.Books.AsNoTracking().ToListAsync();
        }

        // Получить книгу по ID или null, если такой нет.
        // Отслеживание включено, потому что Service изменяет найденную книгу.
        public async Task<Book?> GetByIdAsync(int id)
        {
            return await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
        }

        // Создать книгу: INSERT, после сохранения Id заполняет база.
        public async Task<Book> CreateAsync(Book entity)
        {
            _context.Books.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // Обновить книгу: UPDATE.
        // Book? совпадает с сигнатурой в IBaseRepository.
        public async Task<Book?> UpdateAsync(Book entity)
        {
            _context.Books.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // Удалить книгу по ID.
        // false, если книги нет (Controller вернёт 404).
        public async Task<bool> DeleteAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return false;
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}