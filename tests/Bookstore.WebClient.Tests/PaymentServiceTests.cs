using Bookstore.WebClient.Models.Payment;
using Bookstore.WebClient.Service;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookstore.WebClient.Tests
{
    // Unit-тесты эмуляции оплаты.
    //
    // Unit-тест проверяет один класс отдельно от остального приложения:
    // без API, базы и браузера. Поэтому он выполняется за миллисекунды.
    public class PaymentServiceTests
    {
        // Сервис без паузы "ответа банка". NullLogger — логгер, который ничего не пишет.
        private readonly PaymentService _service = new(NullLogger<PaymentService>.Instance, simulateBankDelay: false);

        private static PaymentRequest ValidRequest(string card = "4242 4242 4242 4242") => new()
        {
            Amount = 500,
            CardNumber = card,
            CardHolder = "IVAN PETRENKO",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 2,
            Cvv = "123"
        };

        [Fact]
        public async Task ValidCard_IsApproved_WithTransactionAndLast4()
        {
            var result = await _service.PayAsync(ValidRequest());

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentStatus.Approved, result.Status);
            Assert.Equal("4242", result.CardLast4);
            Assert.False(string.IsNullOrEmpty(result.TransactionId));
        }

        // [Theory] + [InlineData] — один тест с разными входными данными:
        // xUnit запустит его отдельно для каждой строки InlineData.
        [Theory]
        [InlineData("4000 0000 0000 0002", PaymentStatus.Declined)]
        [InlineData("4000 0000 0000 9995", PaymentStatus.InsufficientFunds)]
        public async Task TestCards_ReturnExpectedStatus(string card, PaymentStatus expected)
        {
            var result = await _service.PayAsync(ValidRequest(card));

            Assert.False(result.IsSuccess);
            Assert.Equal(expected, result.Status);
            Assert.Null(result.TransactionId);
        }

        [Fact]
        public async Task CardFailingLuhnCheck_IsInvalid()
        {
            // Последняя цифра изменена — контрольная сумма по алгоритму Луна не сходится
            var result = await _service.PayAsync(ValidRequest("4242 4242 4242 4241"));

            Assert.Equal(PaymentStatus.InvalidCard, result.Status);
        }

        [Fact]
        public async Task ExpiredCard_IsInvalid()
        {
            var request = ValidRequest();
            request.ExpiryYear = DateTime.UtcNow.Year - 1;

            var result = await _service.PayAsync(request);

            Assert.Equal(PaymentStatus.InvalidCard, result.Status);
        }

        [Theory]
        [InlineData("12")]
        [InlineData("12a")]
        [InlineData("12345")]
        public async Task WrongCvv_IsInvalid(string cvv)
        {
            var request = ValidRequest();
            request.Cvv = cvv;

            var result = await _service.PayAsync(request);

            Assert.Equal(PaymentStatus.InvalidCard, result.Status);
        }

        [Fact]
        public async Task ZeroAmount_IsInvalid()
        {
            var request = ValidRequest();
            request.Amount = 0;

            var result = await _service.PayAsync(request);

            Assert.Equal(PaymentStatus.InvalidCard, result.Status);
        }

        [Fact]
        public async Task TwoDigitYear_IsAccepted()
        {
            var request = ValidRequest();
            request.ExpiryYear = (DateTime.UtcNow.Year + 1) % 100;   // например, 27 вместо 2027

            var result = await _service.PayAsync(request);

            Assert.True(result.IsSuccess);
        }
    }
}
