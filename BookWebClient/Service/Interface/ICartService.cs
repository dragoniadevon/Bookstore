using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Корзина текущего посетителя.
    //
    // Две реализации:
    // - SessionCartService — корзина в сессии (мок-режим и гости);
    // - ApiCartService — корзина в Bookstore.Api (вошедший пользователь в режиме API).
    // Какую использовать, решает SwitchableCartService.
    //
    // Методы асинхронные (Task), потому что корзина в API — это HTTP-запросы.
    public interface ICartService
    {
        Task<List<CartItem>> GetItemsAsync(CancellationToken ct = default);

        // Возвращают текст ошибки (например, "у наявності лише 2 шт.") или null, если всё хорошо
        Task<string?> AddAsync(BookCard book, int quantity = 1, CancellationToken ct = default);
        Task<string?> SetQuantityAsync(int bookId, int quantity, CancellationToken ct = default);

        Task RemoveAsync(int bookId, CancellationToken ct = default);
        Task ClearAsync(CancellationToken ct = default);

        // Сколько всего книг в корзине (для значка в шапке)
        Task<int> CountAsync(CancellationToken ct = default);
    }
}
