using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Models.Api;
using Bookstore.WebClient.Service.Interface;
using System.Net;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Service
{
    // Вход и регистрация через Bookstore.Api.
    //
    // API проверяет пароль и возвращает JWT-токен. Клиент кладёт токен
    // в свою cookie входа (SignInHelper), и ApiGateway добавляет его к запросам.
    public class ApiAuthService : IAuthService
    {
        private readonly IApiGateWay _gateway;

        public ApiAuthService(IApiGateWay gateway)
        {
            _gateway = gateway;
        }

        public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<AuthResponseDto>(new ApiRequest
            {
                ApiType = ApiType.POST,
                URL = "api/Auth/login",
                Data = new LoginRequestDto { Email = email, Password = password }
            }, ct);

            return response.IsSuccess
                ? ToResult(response.Result!)
                : AuthResult.Fail(response.HttpStatusCode == HttpStatusCode.Unauthorized
                    ? "Неправильний email або пароль."
                    : ConnectionError(response));
        }

        public async Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default)
        {
            var response = await _gateway.ApiRequest<AuthResponseDto>(new ApiRequest
            {
                ApiType = ApiType.POST,
                URL = "api/Auth/register",
                Data = new RegisterRequestDto { Name = name, Email = email, Password = password }
            }, ct);

            return response.IsSuccess
                ? ToResult(response.Result!)
                : AuthResult.Fail(response.HttpStatusCode == HttpStatusCode.Conflict
                    ? "Користувач із таким email уже зареєстрований."
                    : ConnectionError(response));
        }

        private static AuthResult ToResult(AuthResponseDto dto) => new()
        {
            User = new ShopUser { Name = dto.User.Name, Email = dto.User.Email, Role = dto.User.Role },
            Token = dto.Token,
            ExpiresAt = dto.ExpiresAt
        };

        private static string ConnectionError<T>(ApiResponse<T> response) =>
            response.HttpStatusCode == 0
                ? "Не вдалося з'єднатися з Bookstore.Api. Перевірте, що API запущено."
                : "Не вдалося виконати запит. Спробуйте ще раз.";
    }

    // Вход и регистрация в мок-режиме: пользователи в памяти (InMemoryUserStore).
    public class MockAuthService : IAuthService
    {
        private readonly IUserStore _users;

        public MockAuthService(IUserStore users)
        {
            _users = users;
        }

        public Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var user = _users.ValidateCredentials(email, password);
            return Task.FromResult(user == null
                ? AuthResult.Fail("Неправильний email або пароль.")
                : new AuthResult { User = user });
        }

        public Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default)
        {
            var user = _users.Register(name, email, password);
            return Task.FromResult(user == null
                ? AuthResult.Fail("Користувач із таким email уже зареєстрований.")
                : new AuthResult { User = user });
        }
    }
}
