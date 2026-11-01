using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Вход и регистрация.
    //
    // Две реализации:
    // - ApiAuthService — через /api/Auth, возвращает JWT-токен;
    // - MockAuthService — пользователи в памяти клиента (мок-режим).
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);
        Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default);
    }
}
