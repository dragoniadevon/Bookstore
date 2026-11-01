namespace Bookstore.WebClient.Models.Payment
{
    // Данные, которые пользователь вводит на странице оплаты.
    // Передаются в PaymentService.PayAsync.
    public class PaymentRequest
    {
        // Какой заказ оплачиваем
        public int OrderId { get; set; }

        // Сумма к оплате
        public decimal Amount { get; set; }

        // Номер карты. Можно с пробелами — сервис сам оставит только цифры.
        // string, а не long: номер карты — это идентификатор, а не число для вычислений
        // (и он может начинаться с нуля).
        public string CardNumber { get; set; } = string.Empty;

        // Имя владельца, как написано на карте
        public string CardHolder { get; set; } = string.Empty;

        // Срок действия: месяц (1–12) и год (можно "28" или "2028")
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        // Код CVV с обратной стороны карты (3–4 цифры)
        public string Cvv { get; set; } = string.Empty;
    }
}
