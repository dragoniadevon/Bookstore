using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service
{
    // Переносит гостевую корзину (из сессии) в корзину пользователя в API после входа.
    //
    // Гость может набрать книги в корзину до входа. Если после входа корзина
    // "пропадёт", это неудобно — поэтому переносим её содержимое в API.
    // В мок-режиме переносить некуда: корзина и так в сессии (токена нет).
    public class CartMerger
    {
        private readonly SessionCartService _session;
        private readonly ApiCartService _api;

        public CartMerger(SessionCartService session, ApiCartService api)
        {
            _session = session;
            _api = api;
        }

        public async Task MergeGuestCartAsync(AuthResult auth, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(auth.Token)) return;

            var guestItems = await _session.GetItemsAsync(ct);
            if (guestItems.Count == 0) return;

            await _api.MergeAsync(guestItems, auth.Token, ct);
            await _session.ClearAsync(ct);
        }
    }
}
