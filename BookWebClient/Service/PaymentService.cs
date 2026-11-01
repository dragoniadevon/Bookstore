using Bookstore.WebClient.Models.Payment;
using Bookstore.WebClient.Service.Interface;

namespace Bookstore.WebClient.Service
{
    // Эмуляция платёжного шлюза. Реальных списаний нет, данные карты никуда не отправляются и не сохраняются.
    //
    // Проверяет данные карты как настоящий шлюз (алгоритм Луна, срок действия, CVV),
    // делает паузу 1–3 секунды и возвращает результат в зависимости от тестовой карты:
    //   4242 4242 4242 4242 — оплата проходит;
    //   4000 0000 0000 0002 — банк отклонил платёж;
    //   4000 0000 0000 9995 — недостаточно средств.
    // Любая другая корректная карта — оплата проходит.
    public class PaymentService : IPaymentService
    {
        // Тестовые номера карт, на которые сервис отвечает отказом.
        private const string DeclinedCard = "4000000000000002";
        private const string InsufficientFundsCard = "4000000000009995";

        // Логгер — пишет сообщения в консоль/лог, чтобы видеть, что происходит в программе.
        // ILogger<PaymentService> — в логе будет видно, что сообщение пришло из этого класса.
        private readonly ILogger<PaymentService> _logger;

        // Логгер передаёт Dependency Injection (ASP.NET Core регистрирует его сам).
        public PaymentService(ILogger<PaymentService> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentResult> PayAsync(PaymentRequest request, CancellationToken ct = default)
        {
            // Убираем из номера всё, кроме цифр: "4242 4242-4242" -> "42424242...".
            // Where(char.IsDigit) — оставить только символы-цифры (это LINQ).
            var cardNumber = new string(request.CardNumber.Where(char.IsDigit).ToArray());

            // Последние 4 цифры. [^4..] — "от четвёртого символа с конца до конца строки".
            var last4 = cardNumber.Length >= 4 ? cardNumber[^4..] : null;

            // Сначала проверяем данные. Нет смысла "звонить в банк" с неверной картой.
            var validationError = Validate(request, cardNumber);
            if (validationError != null)
            {
                return Result(PaymentStatus.InvalidCard, last4, validationError);
            }

            try
            {
                // Имитация ожидания ответа от банка.
                await Task.Delay(Random.Shared.Next(1000, 3000), ct);
            }
            // Если пользователь закрыл страницу во время ожидания, Task.Delay
            // бросает TaskCanceledException. Ловим его и сообщаем об отмене.
            catch (TaskCanceledException)
            {
                return Result(PaymentStatus.Cancelled, last4, "Оплату скасовано.");
            }

            // switch-выражение: сравниваем номер карты с тестовыми номерами.
            // "_" означает "любое другое значение" (как default в обычном switch).
            var result = cardNumber switch
            {
                DeclinedCard => Result(PaymentStatus.Declined, last4, "Банк відхилив платіж."),
                InsufficientFundsCard => Result(PaymentStatus.InsufficientFunds, last4, "Недостатньо коштів на картці."),
                _ => Result(PaymentStatus.Approved, last4, "Оплата пройшла успішно.", Guid.NewGuid().ToString("N"))
            };

            // Пишем в лог результат. {OrderId} и т.д. — это не интерполяция строк,
            // а "структурированный лог": значения сохраняются отдельными полями,
            // и потом по ним удобно искать. Полный номер карты в лог не пишем никогда.
            _logger.LogInformation(
                "Оплата заказа {OrderId} на сумму {Amount}: {Status}, карта *{Last4}",
                request.OrderId, request.Amount, result.Status, last4);

            return result;
        }

        // Проверка данных карты.
        // Возвращает текст ошибки или null, если всё в порядке.
        // static — методу не нужны поля объекта (_logger и т.п.), только параметры.
        private static string? Validate(PaymentRequest request, string cardNumber)
        {
            // "is < 13 or > 19" — короткая запись "меньше 13 ИЛИ больше 19" (pattern matching).
            if (request.Amount <= 0)
                return "Сума оплати має бути більшою за нуль.";

            if (cardNumber.Length is < 13 or > 19 || !PassesLuhn(cardNumber))
                return "Неправильний номер картки.";

            if (string.IsNullOrWhiteSpace(request.CardHolder))
                return "Вкажіть ім'я власника картки.";

            if (request.ExpiryMonth is < 1 or > 12)
                return "Неправильний місяць терміну дії.";

            // Год могут ввести как "28" или "2028" — приводим к четырём цифрам.
            // Карта действует до конца указанного месяца, поэтому сравниваем
            // с первым днём СЛЕДУЮЩЕГО месяца.
            var year = request.ExpiryYear < 100 ? 2000 + request.ExpiryYear : request.ExpiryYear;
            var expiry = new DateTime(year, request.ExpiryMonth, 1).AddMonths(1);
            if (expiry <= DateTime.UtcNow)
                return "Термін дії картки минув.";

            if (request.Cvv.Length is < 3 or > 4 || !request.Cvv.All(char.IsDigit))
                return "Неправильний CVV.";

            return null;
        }

        // Алгоритм Луна — стандартная проверка контрольной цифры номера карты.
        //
        // Последняя цифра номера карты не случайная: она вычисляется из остальных.
        // Это ловит опечатки — если ошибиться в одной цифре, проверка не пройдёт.
        //
        // Как работает: идём по цифрам справа налево, каждую вторую удваиваем
        // (если получилось больше 9 — вычитаем 9), всё складываем.
        // У правильного номера сумма делится на 10 без остатка.
        private static bool PassesLuhn(string digits)
        {
            var sum = 0;
            var doubleDigit = false;

            for (var i = digits.Length - 1; i >= 0; i--)
            {
                var d = digits[i] - '0';
                if (doubleDigit)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
                doubleDigit = !doubleDigit;
            }

            return sum % 10 == 0;
        }

        // Вспомогательный метод: собирает PaymentResult, чтобы не повторять
        // одни и те же четыре строки в каждом месте.
        private static PaymentResult Result(PaymentStatus status, string? last4, string message, string? transactionId = null) => new()
        {
            Status = status,
            CardLast4 = last4,
            Message = message,
            TransactionId = transactionId
        };
    }
}
