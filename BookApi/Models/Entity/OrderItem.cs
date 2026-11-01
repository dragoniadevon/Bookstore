using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность OrderItem — одна позиция в заказе.
    //
    // Похожа на CartItem, но с важным отличием: здесь хранится UnitPrice —
    // цена книги В МОМЕНТ покупки. Если завтра цена книги изменится,
    // старые заказы должны показывать ту сумму, которую человек реально заплатил.
    public class OrderItem : BaseEntity
    {

        // Внешний ключ: Id заказа
        public int OrderId { get; set; }

        // Навигационное свойство: заказ, к которому относится позиция
        public Order Order { get; set; } = null!;

        // Внешний ключ: Id купленной книги
        public int BookId { get; set; }

        // Навигационное свойство: купленная книга
        public Book Book { get; set; } = null!;

        // Сколько экземпляров купили
        public int Quantity { get; set; }

        // Цена за один экземпляр на момент покупки
        public decimal UnitPrice { get; set; }
    }
}
