using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Корзина текущего посетителя.
    public interface ICartService
    {
        List<CartItem> GetItems();
        void Add(BookCard book, int quantity = 1);
        void SetQuantity(int bookId, int quantity);
        void Remove(int bookId);
        void Clear();

        // Сколько всего книг в корзине (для значка в шапке)
        int Count { get; }

        // Общая сумма
        decimal Total { get; }
    }
}
