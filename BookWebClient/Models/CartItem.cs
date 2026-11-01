namespace Bookstore.WebClient.Models
{
    // Одна позиция в корзине: какая книга и сколько штук.
    //
    // Название, автора и цену копируем сюда при добавлении, чтобы корзину
    // можно было показать без лишних запросов к API.
    public class CartItem
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        // Сколько штук есть на складе — больше этого положить в корзину нельзя
        public int MaxQuantity { get; set; }

        // Стоимость позиции: цена * количество
        public decimal Total => Price * Quantity;
    }
}
