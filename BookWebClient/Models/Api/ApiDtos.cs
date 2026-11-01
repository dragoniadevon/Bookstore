namespace Bookstore.WebClient.Models.Api
{
    // Объекты, которыми клиент обменивается с /api/Auth, /api/Cart и /api/Order.
    // Это "зеркало" DTO из Bookstore.Api: имена и типы полей должны совпадать.

    // ---------- Авторизация ----------

    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApiUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // Ответ на вход и регистрацию: JWT-токен и данные пользователя
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public ApiUserDto User { get; set; } = new();
    }

    // ---------- Корзина ----------

    public class ApiCartDto
    {
        public List<ApiCartItemDto> Items { get; set; } = [];
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ApiCartItemDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public decimal Total { get; set; }
    }

    public class AddCartItemRequestDto
    {
        public int BookId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequestDto
    {
        public int Quantity { get; set; }
    }

    // ---------- Заказы ----------

    public class CreateOrderRequestDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? CardLast4 { get; set; }
        public string? TransactionId { get; set; }
    }

    public class ApiOrderDto
    {
        public string Number { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? CardLast4 { get; set; }
        public string? TransactionId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ApiOrderItemDto> Items { get; set; } = [];
    }

    public class ApiOrderItemDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
