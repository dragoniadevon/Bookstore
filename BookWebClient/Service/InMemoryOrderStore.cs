using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using System.Collections.Concurrent;

namespace Bookstore.WebClient.Service
{
    // Демо-хранилище заказов в памяти (Singleton). Пропадает при перезапуске.
    // Когда в API появятся заказы, его заменит реализация, которая ходит в API.
    public class InMemoryOrderStore : IOrderStore
    {
        private readonly ConcurrentBag<Order> _orders = new();

        public void Add(Order order) => _orders.Add(order);

        public List<Order> GetByCustomer(string email) => _orders
            .Where(o => o.CustomerEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        public Order? Find(string email, string number) =>
            GetByCustomer(email).FirstOrDefault(o => o.Number == number);
    }
}
