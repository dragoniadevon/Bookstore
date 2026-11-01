using Bookstore.Api.Data;
using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Services
{
    // Оформление заказов и история покупок.
    internal class OrderService : IOrderService
    {
        private readonly AppDbContext _db;

        public OrderService(AppDbContext db)
        {
            _db = db;
        }

        // Оформить заказ из корзины пользователя.
        //
        // Всё выполняется в одной транзакции: либо получилось всё сразу
        // (склад уменьшен, заказ сохранён, корзина очищена), либо ничего.
        // Если на середине что-то пошло не так, база вернётся к исходному состоянию.
        public async Task<ServiceResult<OrderDTO>> PlaceOrderAsync(int userId, CreateOrderDTO dto)
        {
            // await using — транзакция будет закрыта (и откатится, если не было Commit)
            // при выходе из метода, даже если произойдёт исключение.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var cart = await _db.Carts
                .Include(c => c.Items).ThenInclude(i => i.Book)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.Items.Count == 0)
            {
                return ServiceResult<OrderDTO>.Fail(ServiceStatus.BadRequest, "Кошик порожній.");
            }

            var now = DateTime.UtcNow;

            foreach (var item in cart.Items)
            {
                // Уменьшаем остаток ОДНОЙ SQL-командой с условием:
                //   UPDATE Books SET StockQuantity = StockQuantity - @qty
                //   WHERE Id = @id AND StockQuantity >= @qty
                //
                // Почему не "прочитать остаток, проверить, записать"? Если два покупателя
                // оформляют заказ одновременно, оба могут прочитать "осталась 1 книга"
                // и оба её "купить". Условие в самом UPDATE проверяется базой атомарно:
                // второй запрос просто не найдёт подходящую строку.
                var updated = await _db.Books
                    .Where(b => b.Id == item.BookId && b.StockQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.StockQuantity, b => b.StockQuantity - item.Quantity)
                        .SetProperty(b => b.ModifiedAt, now));

                if (updated == 0)
                {
                    // Книг не хватило — транзакция откатится, ничего не изменится
                    var current = await _db.Books.AsNoTracking().FirstAsync(b => b.Id == item.BookId);
                    return ServiceResult<OrderDTO>.Fail(ServiceStatus.Conflict, CartService.StockError(current));
                }
            }

            var order = new Order
            {
                Number = GenerateNumber(now),
                UserId = userId,
                Status = OrderStatuses.Paid,
                OrderDate = now,
                CustomerName = dto.CustomerName.Trim(),
                Phone = dto.Phone.Trim(),
                Address = dto.Address.Trim(),
                CardLast4 = dto.CardLast4,
                TransactionId = dto.TransactionId,

                // Цена "замораживается" на момент покупки: если завтра книга подорожает,
                // в истории заказов останется та сумма, которую покупатель заплатил.
                OrderItems = cart.Items.Select(i => new OrderItem
                {
                    BookId = i.BookId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Book.Price
                }).ToList()
            };
            order.TotalAmount = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);

            _db.Orders.Add(order);

            // Корзина после оформления очищается
            _db.CartItems.RemoveRange(cart.Items);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<OrderDTO>.Ok((await GetByNumberAsync(userId, order.Number))!);
        }

        public async Task<List<OrderDTO>> GetMyOrdersAsync(int userId)
        {
            var orders = await OrdersWithItems()
                .Where(o => o.UserId == userId)
                .ToListAsync();

            // Сортируем в памяти: новые заказы сверху
            return orders.OrderByDescending(o => o.OrderDate).Select(ToDto).ToList();
        }

        // Ищем только среди заказов этого пользователя: чужой заказ по номеру
        // не найдётся (вернём null -> 404), даже если номер угадать.
        public async Task<OrderDTO?> GetByNumberAsync(int userId, string number)
        {
            var order = await OrdersWithItems()
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Number == number);
            return order == null ? null : ToDto(order);
        }

        private IQueryable<Order> OrdersWithItems() => _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(i => i.Book);

        // Номер вида "BS-20261102-483920": дата + случайные 6 цифр
        private static string GenerateNumber(DateTime now) => $"BS-{now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        private static OrderDTO ToDto(Order order) => new()
        {
            Number = order.Number,
            OrderDate = order.OrderDate,
            Status = order.Status,
            CustomerName = order.CustomerName,
            Phone = order.Phone,
            Address = order.Address,
            CardLast4 = order.CardLast4,
            TransactionId = order.TransactionId,
            TotalAmount = order.TotalAmount,
            Items = order.OrderItems
                .OrderBy(i => i.Id)
                .Select(i => new OrderItemDTO
                {
                    BookId = i.BookId,
                    Title = i.Book.Title,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Total = i.UnitPrice * i.Quantity
                })
                .ToList()
        };
    }
}
