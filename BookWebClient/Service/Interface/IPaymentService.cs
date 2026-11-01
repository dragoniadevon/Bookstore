using Bookstore.WebClient.Models.Payment;

namespace Bookstore.WebClient.Service.Interface
{
    // Интерфейс сервиса оплаты.
    //
    // Сейчас его реализует PaymentService — эмуляция без реальных денег.
    // Когда (если) подключат настоящую платёжную систему, достаточно написать
    // новый класс с этим же интерфейсом и поменять одну строку в Program.cs.
    // Страницы, которые вызывают оплату, менять не придётся.
    public interface IPaymentService
    {
        // Провести оплату. Никогда не бросает исключение — результат всегда в PaymentResult.
        Task<PaymentResult> PayAsync(PaymentRequest request, CancellationToken ct = default);
    }
}
