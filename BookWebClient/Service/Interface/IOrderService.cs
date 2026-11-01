using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Оформление заказа и история заказов текущего пользователя.
    //
    // Две реализации:
    // - ApiOrderService — через /api/Order (склад уменьшается в базе);
    // - MockOrderService — заказы в памяти клиента (мок-режим).
    public interface IOrderService
    {
        // Оплатить и оформить заказ из корзины
        Task<OrderResult> PlaceOrderAsync(CheckoutData data, CancellationToken ct = default);

        Task<List<Order>> GetMyOrdersAsync(CancellationToken ct = default);
        Task<Order?> GetAsync(string number, CancellationToken ct = default);
    }
}
