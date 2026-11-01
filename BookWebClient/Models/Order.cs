namespace Bookstore.WebClient.Models
{
    // Оформленный и оплаченный заказ.
    //
    // В API пока нет заказов, поэтому для демонстрации они хранятся
    // в памяти WebClient (InMemoryOrderStore) и пропадают при перезапуске.
    public class Order
    {
        // Номер заказа для покупателя, например "BS-20261101-4821"
        public string Number { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Email покупателя — по нему находим заказы в "Моих заказах"
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public List<CartItem> Items { get; set; } = [];
        public decimal Total { get; set; }

        // Данные об оплате: последние 4 цифры карты и номер транзакции
        public string? CardLast4 { get; set; }
        public string? TransactionId { get; set; }
    }
}
