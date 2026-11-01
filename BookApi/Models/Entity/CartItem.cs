using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность CartItem — одна позиция в корзине.
    //
    // Это "связующая" таблица между Cart и Book.
    // Она нужна, потому что связь корзины и книг — "многие ко многим":
    // в одной корзине много книг, и одна книга может лежать во многих корзинах.
    // Кроме того, здесь хранится количество — его негде было бы хранить
    // при прямой связи Cart <-> Book.
    //
    // Пример: "в корзине №5 лежат 2 экземпляра книги №12".
    public class CartItem : BaseEntity
    {
        // Внешний ключ: Id корзины
        public int CartId { get; set; }

        // Навигационное свойство: корзина, к которой относится позиция
        // (обратная сторона — Cart.Items)
        public Cart Cart { get; set; } = null!;

        // Внешний ключ: Id книги
        public int BookId { get; set; }

        // Навигационное свойство: книга
        public Book Book { get; set; } = null!;

        // Количество экземпляров этой книги в корзине
        public int Quantity { get; set; }
    }
}
