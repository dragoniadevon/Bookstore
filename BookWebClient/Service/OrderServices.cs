using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Models.Api;
using Bookstore.WebClient.Models.Payment;
using Bookstore.WebClient.Service.Interface;
using Bookstore.WebClient.Service.Mock;
using System.Net;
using System.Security.Claims;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Service
{
    // Заказы через Bookstore.Api.
    //
    // Порядок оформления:
    // 1. Берём корзину из API и проверяем, что она не пуста и книг хватает.
    // 2. Проводим оплату в платёжной системе (PaymentService — эмуляция).
    //    Как и в настоящих магазинах, данные карты уходят только в платёжную систему,
    //    а наш API получает лишь последние 4 цифры и номер транзакции.
    // 3. POST /api/Order — API в одной транзакции уменьшает склад, сохраняет заказ
    //    и очищает корзину. Если за это время книги закончились, API ответит 409.
    public class ApiOrderService : IOrderService
    {
        private const string Endpoint = "api/Order";

        private readonly IApiGateWay _gateway;
        private readonly ApiCartService _cart;
        private readonly IPaymentService _payment;

        public ApiOrderService(IApiGateWay gateway, ApiCartService cart, IPaymentService payment)
        {
            _gateway = gateway;
            _cart = cart;
            _payment = payment;
        }

        public async Task<OrderResult> PlaceOrderAsync(CheckoutData data, CancellationToken ct = default)
        {
            // 1. Корзина и остатки
            var items = await _cart.GetItemsAsync(ct);
            if (items.Count == 0) return OrderResult.Fail("Кошик порожній.");

            var shortage = items.FirstOrDefault(i => i.Quantity > i.MaxQuantity);
            if (shortage != null)
            {
                return OrderResult.Fail($"«{shortage.Title}»: у наявності лише {shortage.MaxQuantity} шт. Змініть кількість у кошику.");
            }

            // 2. Оплата
            var payment = await _payment.PayAsync(ToPaymentRequest(data, items.Sum(i => i.Total)), ct);
            if (!payment.IsSuccess) return OrderResult.Fail(payment.Message);

            // 3. Заказ в API
            var response = await _gateway.ApiRequest<ApiOrderDto>(new ApiRequest
            {
                ApiType = ApiType.POST,
                URL = Endpoint,
                Data = new CreateOrderRequestDto
                {
                    CustomerName = data.FullName,
                    Phone = data.Phone,
                    Address = data.Address,
                    CardLast4 = payment.CardLast4,
                    TransactionId = payment.TransactionId
                }
            }, ct);

            if (!response.IsSuccess)
            {
                // В настоящем магазине здесь был бы возврат денег через платёжную систему
                var reason = response.HttpStatusCode == HttpStatusCode.Conflict && !string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? response.ErrorMessage
                    : response.ErrorMessage ?? "Не вдалося оформити замовлення.";
                return OrderResult.Fail($"Замовлення не оформлено, оплату скасовано. {reason}");
            }

            return new OrderResult { Order = ToOrder(response.Result!) };
        }

        public async Task<List<Order>> GetMyOrdersAsync(CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<List<ApiOrderDto>>(new ApiRequest { ApiType = ApiType.GET, URL = Endpoint }, ct);
            return response.IsSuccess && response.Result != null ? response.Result.Select(ToOrder).ToList() : [];
        }

        public async Task<Order?> GetAsync(string number, CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<ApiOrderDto>(new ApiRequest
            {
                ApiType = ApiType.GET,
                URL = $"{Endpoint}/{Uri.EscapeDataString(number)}"
            }, ct);
            return response.IsSuccess && response.Result != null ? ToOrder(response.Result) : null;
        }

        internal static PaymentRequest ToPaymentRequest(CheckoutData data, decimal amount) => new()
        {
            Amount = amount,
            CardNumber = data.CardNumber,
            CardHolder = data.CardHolder,
            ExpiryMonth = data.ExpiryMonth,
            ExpiryYear = data.ExpiryYear,
            Cvv = data.Cvv
        };

        internal static Order ToOrder(ApiOrderDto dto) => new()
        {
            Number = dto.Number,
            // API хранит время в UTC, показываем покупателю местное время
            CreatedAt = DateTime.SpecifyKind(dto.OrderDate, DateTimeKind.Utc).ToLocalTime(),
            CustomerName = dto.CustomerName,
            Phone = dto.Phone,
            Address = dto.Address,
            CardLast4 = dto.CardLast4,
            TransactionId = dto.TransactionId,
            Total = dto.TotalAmount,
            Items = dto.Items.Select(i => new CartItem
            {
                BookId = i.BookId,
                Title = i.Title,
                Price = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    // Заказы в мок-режиме: корзина из сессии, заказы в памяти (InMemoryOrderStore),
    // склад уменьшается в мок-хранилище MockDataStore.
    public class MockOrderService : IOrderService
    {
        private readonly SessionCartService _cart;
        private readonly IPaymentService _payment;
        private readonly IOrderStore _orders;
        private readonly MockDataStore _store;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MockOrderService(SessionCartService cart, IPaymentService payment, IOrderStore orders,
            MockDataStore store, IHttpContextAccessor httpContextAccessor)
        {
            _cart = cart;
            _payment = payment;
            _orders = orders;
            _store = store;
            _httpContextAccessor = httpContextAccessor;
        }

        private string CurrentEmail =>
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        public async Task<OrderResult> PlaceOrderAsync(CheckoutData data, CancellationToken ct = default)
        {
            var items = await _cart.GetItemsAsync(ct);
            if (items.Count == 0) return OrderResult.Fail("Кошик порожній.");

            var total = items.Sum(i => i.Total);

            // Проверяем остатки ДО оплаты
            var stockError = CheckStock(items);
            if (stockError != null) return OrderResult.Fail(stockError);

            var payment = await _payment.PayAsync(ApiOrderService.ToPaymentRequest(data, total), ct);
            if (!payment.IsSuccess) return OrderResult.Fail(payment.Message);

            // Уменьшаем остатки (под блокировкой — данные общие для всех посетителей).
            // Пока шла оплата, книги мог купить кто-то другой — проверяем ещё раз.
            lock (_store.SyncRoot)
            {
                stockError = CheckStock(items);
                if (stockError != null) return OrderResult.Fail($"Замовлення не оформлено, оплату скасовано. {stockError}");

                var books = _store.Table<BookDto>();
                foreach (var item in items)
                {
                    books.First(b => b.Id == item.BookId).StockQuantity -= item.Quantity;
                }
            }

            var order = new Order
            {
                Number = $"BS-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}",
                CustomerEmail = CurrentEmail,
                CustomerName = data.FullName,
                Phone = data.Phone,
                Address = data.Address,
                Items = items,
                Total = total,
                CardLast4 = payment.CardLast4,
                TransactionId = payment.TransactionId
            };

            _orders.Add(order);
            await _cart.ClearAsync(ct);
            return new OrderResult { Order = order };
        }

        public Task<List<Order>> GetMyOrdersAsync(CancellationToken ct = default) =>
            Task.FromResult(_orders.GetByCustomer(CurrentEmail));

        public Task<Order?> GetAsync(string number, CancellationToken ct = default) =>
            Task.FromResult(_orders.Find(CurrentEmail, number));

        private string? CheckStock(List<CartItem> items)
        {
            lock (_store.SyncRoot)
            {
                var books = _store.Table<BookDto>();
                foreach (var item in items)
                {
                    var book = books.FirstOrDefault(b => b.Id == item.BookId);
                    if (book == null) return $"Книги «{item.Title}» більше немає в каталозі.";
                    if (book.StockQuantity < item.Quantity) return $"«{item.Title}»: у наявності лише {book.StockQuantity} шт.";
                }
            }
            return null;
        }
    }
}
