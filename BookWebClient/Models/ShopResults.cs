namespace Bookstore.WebClient.Models
{
    // Результат входа или регистрации
    public class AuthResult
    {
        public bool Success => User != null;

        // Кто вошёл (имя, email, роль)
        public ShopUser? User { get; init; }

        // JWT-токен от API (в мок-режиме — null: токен не нужен)
        public string? Token { get; init; }
        public DateTime? ExpiresAt { get; init; }

        // Текст ошибки для пользователя
        public string? Error { get; init; }

        public static AuthResult Fail(string error) => new() { Error = error };
    }

    // Что вводит покупатель при оформлении заказа
    public class CheckoutData
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Данные карты — только для эмуляции платёжной системы.
        // В API уходят лишь последние 4 цифры и номер транзакции.
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolder { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string Cvv { get; set; } = string.Empty;
    }

    // Результат оформления заказа
    public class OrderResult
    {
        public bool Success => Order != null;
        public Order? Order { get; init; }
        public string? Error { get; init; }

        public static OrderResult Fail(string error) => new() { Error = error };
    }
}
