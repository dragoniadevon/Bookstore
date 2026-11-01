using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Order — оформленный и оплаченный заказ.
    //
    // Создаётся из корзины пользователя (см. OrderService.PlaceOrderAsync).
    // Сам заказ хранит общую информацию (кто, когда, сколько, куда доставить),
    // а купленные книги лежат в отдельной таблице OrderItems.
    public class Order : BaseEntity
    {
        // Номер заказа для покупателя, например "BS-20261102-483920".
        // Уникален (индекс в AppDbContext). Id — внутренний номер базы, его наружу не показываем.
        public string Number { get; set; } = string.Empty;

        // Внешний ключ: Id пользователя, который сделал заказ
        public int UserId { get; set; }

        // Навигационное свойство: покупатель
        public User User { get; set; } = null!;

        // Общая сумма заказа
        public decimal TotalAmount { get; set; }

        // Статус заказа. Пока только "Paid" (оплачен) — см. OrderStatuses.
        public string Status { get; set; } = string.Empty;

        // Дата оформления заказа (UTC)
        public DateTime OrderDate { get; set; }

        // Контактные данные и адрес доставки
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Данные об оплате: последние 4 цифры карты и номер транзакции платёжной системы.
        // Полный номер карты в API никогда не передаётся и не хранится.
        public string? CardLast4 { get; set; }
        public string? TransactionId { get; set; }

        // Навигационное свойство: позиции заказа (какие книги и сколько).
        // "= new List<OrderItem>()" — чтобы список никогда не был null
        // и в него можно было сразу добавлять элементы.
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    // Возможные статусы заказа
    public static class OrderStatuses
    {
        public const string Paid = "Paid";
    }
}
