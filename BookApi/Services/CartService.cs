using Bookstore.Api.Data;
using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Services
{
    // Корзина пользователя.
    //
    // Работает с AppDbContext напрямую: одна операция затрагивает сразу несколько
    // таблиц (Carts, CartItems, Books), а общий IBaseRepository умеет только
    // простые действия с одной таблицей.
    internal class CartService : ICartService
    {
        private readonly AppDbContext _db;

        public CartService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CartDTO> GetAsync(int userId)
        {
            var cart = await LoadCartAsync(userId);
            return ToDto(cart);
        }

        public async Task<ServiceResult<CartDTO>> AddItemAsync(int userId, AddCartItemDTO dto)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == dto.BookId);
            if (book == null)
            {
                return ServiceResult<CartDTO>.Fail(ServiceStatus.NotFound, "Книгу не знайдено.");
            }

            var cart = await LoadCartAsync(userId) ?? CreateCart(userId);
            var item = cart.Items.FirstOrDefault(i => i.BookId == dto.BookId);
            var newQuantity = (item?.Quantity ?? 0) + dto.Quantity;

            // Нельзя положить в корзину больше, чем есть на складе
            if (newQuantity > book.StockQuantity)
            {
                return ServiceResult<CartDTO>.Fail(ServiceStatus.Conflict, StockError(book));
            }

            if (item == null)
            {
                cart.Items.Add(new CartItem { BookId = book.Id, Book = book, Quantity = newQuantity });
            }
            else
            {
                item.Quantity = newQuantity;
            }

            await _db.SaveChangesAsync();
            return ServiceResult<CartDTO>.Ok(ToDto(await LoadCartAsync(userId)));
        }

        public async Task<ServiceResult<CartDTO>> SetQuantityAsync(int userId, int bookId, int quantity)
        {
            var cart = await LoadCartAsync(userId);
            var item = cart?.Items.FirstOrDefault(i => i.BookId == bookId);
            if (item == null)
            {
                return ServiceResult<CartDTO>.Fail(ServiceStatus.NotFound, "Цієї книги немає в кошику.");
            }

            if (quantity <= 0)
            {
                // 0 — убрать позицию из корзины
                _db.CartItems.Remove(item);
            }
            else if (quantity > item.Book.StockQuantity)
            {
                return ServiceResult<CartDTO>.Fail(ServiceStatus.Conflict, StockError(item.Book));
            }
            else
            {
                item.Quantity = quantity;
            }

            await _db.SaveChangesAsync();
            return ServiceResult<CartDTO>.Ok(ToDto(await LoadCartAsync(userId)));
        }

        public Task<ServiceResult<CartDTO>> RemoveItemAsync(int userId, int bookId) => SetQuantityAsync(userId, bookId, 0);

        public async Task ClearAsync(int userId)
        {
            // ExecuteDeleteAsync — удалить строки одной SQL-командой DELETE,
            // не загружая их в память
            await _db.CartItems.Where(i => i.Cart.UserId == userId).ExecuteDeleteAsync();
        }

        // Загрузить корзину вместе с позициями, книгами и авторами.
        // Include — "подгрузи связанные данные" (иначе навигационные свойства будут пустыми).
        private Task<Cart?> LoadCartAsync(int userId) => _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Book).ThenInclude(b => b.Author)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        private Cart CreateCart(int userId)
        {
            var cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            return cart;
        }

        internal static string StockError(Book book) => book.StockQuantity == 0
            ? $"«{book.Title}» зараз немає в наявності."
            : $"«{book.Title}»: у наявності лише {book.StockQuantity} шт.";

        // Entity -> DTO. Суммы считаем здесь, в C#, а не в базе.
        private static CartDTO ToDto(Cart? cart)
        {
            var items = (cart?.Items ?? [])
                .OrderBy(i => i.Id)
                .Select(i => new CartItemDTO
                {
                    BookId = i.BookId,
                    Title = i.Book.Title,
                    AuthorName = i.Book.Author?.Name ?? string.Empty,
                    ImageUrl = i.Book.ImageUrl,
                    Price = i.Book.Price,
                    Quantity = i.Quantity,
                    StockQuantity = i.Book.StockQuantity,
                    Total = i.Book.Price * i.Quantity
                })
                .ToList();

            return new CartDTO
            {
                Items = items,
                TotalQuantity = items.Sum(i => i.Quantity),
                TotalAmount = items.Sum(i => i.Total)
            };
        }
    }
}
