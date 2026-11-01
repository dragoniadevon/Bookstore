using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Хранилище оформленных заказов.
    public interface IOrderStore
    {
        void Add(Order order);

        // Заказы покупателя, новые сверху
        List<Order> GetByCustomer(string email);

        // Один заказ покупателя по номеру (чужие заказы не возвращаются)
        Order? Find(string email, string number);
    }
}
