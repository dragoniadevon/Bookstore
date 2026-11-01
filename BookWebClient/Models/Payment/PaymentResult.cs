namespace Bookstore.WebClient.Models.Payment
{
    // Возможные итоги оплаты.
    public enum PaymentStatus
    {
        Approved,          // оплата прошла
        Declined,          // банк отклонил платёж
        InsufficientFunds, // недостаточно средств
        InvalidCard,       // неверные данные карты (номер, срок, CVV)
        Cancelled          // пользователь прервал оплату (закрыл страницу)
    }

    // Результат оплаты. Полный номер карты сюда не попадает — только последние 4 цифры.
    public class PaymentResult
    {
        // Итог оплаты
        public PaymentStatus Status { get; set; }

        // Вычисляемое свойство: значение не хранится, а каждый раз считается из Status.
        // "=>" здесь означает "возвращает". Удобно писать if (result.IsSuccess).
        public bool IsSuccess => Status == PaymentStatus.Approved;

        // Номер транзакции. Есть только у успешной оплаты —
        // по нему потом можно найти платёж (например, для возврата денег).
        public string? TransactionId { get; set; }

        // Последние 4 цифры карты — чтобы показать пользователю "Оплачено картой *4242"
        public string? CardLast4 { get; set; }

        // Понятный текст для пользователя: "Оплата прошла успешно", "Срок действия карты истёк"...
        public string Message { get; set; } = string.Empty;

        // Когда была обработана оплата.
        // UtcNow — время по Гринвичу, чтобы не путаться с часовыми поясами.
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
