using Bookstore.WebClient.Models;
using Bookstore.WebClient.Models.Payment;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BookWebClient.Pages.Checkout
{
    // Оформление заказа: /Checkout
    // Доступ только для вошедших пользователей (AuthorizeFolder("/Checkout") в Program.cs).
    public class IndexModel : PageModel
    {
        private readonly ICartService _cart;
        private readonly IPaymentService _payment;
        private readonly IOrderStore _orders;

        public IndexModel(ICartService cart, IPaymentService payment, IOrderStore orders)
        {
            _cart = cart;
            _payment = payment;
            _orders = orders;
        }

        // [BindProperty] — при POST поля формы автоматически попадут в Input.
        [BindProperty]
        public CheckoutInput Input { get; set; } = new();

        public List<CartItem> Items { get; private set; } = [];
        public decimal Total { get; private set; }

        public IActionResult OnGet()
        {
            if (!LoadCart()) return RedirectToPage("/Cart/Index");

            // Подставляем имя из профиля, чтобы не вводить его заново
            Input.FullName = User.Identity?.Name ?? string.Empty;
            Input.CardHolder = Input.FullName.ToUpperInvariant();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!LoadCart()) return RedirectToPage("/Cart/Index");

            // ModelState.IsValid — прошли ли все проверки [Required], [RegularExpression] и т.д.
            // Если нет — показываем форму снова, ошибки выведутся под полями.
            if (!ModelState.IsValid) return Page();

            // "08/28" -> месяц 8, год 28
            var expiry = Input.Expiry.Split('/');
            var orderNumber = $"BS-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var result = await _payment.PayAsync(new PaymentRequest
            {
                OrderId = Math.Abs(orderNumber.GetHashCode()),
                Amount = Total,
                CardNumber = Input.CardNumber,
                CardHolder = Input.CardHolder,
                ExpiryMonth = int.Parse(expiry[0]),
                ExpiryYear = int.Parse(expiry[1]),
                Cvv = Input.Cvv
            }, ct);

            if (!result.IsSuccess)
            {
                // Ошибка без привязки к полю ("") выводится в общем блоке ошибок формы
                ModelState.AddModelError(string.Empty, result.Message);
                return Page();
            }

            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            _orders.Add(new Order
            {
                Number = orderNumber,
                CustomerEmail = email,
                CustomerName = Input.FullName,
                Phone = Input.Phone,
                Address = Input.Address,
                Items = Items,
                Total = Total,
                CardLast4 = result.CardLast4,
                TransactionId = result.TransactionId
            });

            _cart.Clear();
            return RedirectToPage("/Checkout/Success", new { number = orderNumber });
        }

        private bool LoadCart()
        {
            Items = _cart.GetItems();
            Total = Items.Sum(i => i.Total);
            return Items.Count > 0;
        }
    }

    // Поля формы оформления заказа и правила их проверки.
    public class CheckoutInput
    {
        [Required(ErrorMessage = "Вкажіть ім'я та прізвище")]
        [Display(Name = "Ім'я та прізвище")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть телефон")]
        [Phone(ErrorMessage = "Неправильний формат телефону")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть адресу доставки")]
        [Display(Name = "Адреса доставки")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть номер картки")]
        [Display(Name = "Номер картки")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть власника картки")]
        [Display(Name = "Власник картки")]
        public string CardHolder { get; set; } = string.Empty;

        // Регулярное выражение: две цифры месяца 01–12, "/", две цифры года
        [Required(ErrorMessage = "Вкажіть термін дії")]
        [RegularExpression(@"^(0[1-9]|1[0-2])/\d{2}$", ErrorMessage = "Формат ММ/РР, наприклад 08/28")]
        [Display(Name = "Термін дії")]
        public string Expiry { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "3 або 4 цифри")]
        [Display(Name = "CVV")]
        public string Cvv { get; set; } = string.Empty;
    }
}
