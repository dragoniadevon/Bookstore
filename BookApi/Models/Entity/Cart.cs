using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность Cart — корзина покупателя.
    //
    // У каждого пользователя одна корзина (связь "один к одному" с User).
    // Сами товары в корзине хранятся в отдельной таблице CartItems:
    // одна строка CartItem = одна книга + её количество.
    public class Cart : BaseEntity
    {
        // Навигационное свойство: позиции корзины (какие книги и сколько).
        //
        // Раньше здесь было IEnumerable<Book> Books, и EF Core из-за этого
        // добавил в таблицу Books колонку CartId — как будто книга "принадлежит"
        // одной корзине. Правильная связь — через CartItem: одна и та же книга
        // может лежать во многих корзинах, и у каждой позиции своё количество.
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

        // Внешний ключ: Id пользователя, которому принадлежит корзина
        public int UserId { get; set; }

        // Навигационное свойство: владелец корзины
        public User User { get; set; } = null!;

    }
}
