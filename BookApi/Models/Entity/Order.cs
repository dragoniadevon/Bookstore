using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Order — оформленный заказ.
    //
    // Когда пользователь оплачивает корзину, из неё создаётся заказ.
    // Сам заказ хранит общую информацию (кто, когда, сколько, статус),
    // а купленные книги лежат в отдельной таблице OrderItems.
    public class Order : BaseEntity
    {
        // Внешний ключ: Id пользователя, который сделал заказ
        public int UserId { get; set; }

        // Навигационное свойство: покупатель
        public User User { get; set; } = null!;

        // Общая сумма заказа
        public decimal TotalAmount { get; set; }

        // Статус заказа, например "Создан", "Оплачен", "Доставлен".
        // Позже лучше заменить строку на enum, чтобы нельзя было записать опечатку.
        public string Status { get; set; } = string.Empty;

        // Дата оформления заказа
        public DateTime OrderDate { get; set; }


        // Навигационное свойство: позиции заказа (какие книги и сколько).
        // "= new List<OrderItem>()" — чтобы список никогда не был null
        // и в него можно было сразу добавлять элементы.
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
