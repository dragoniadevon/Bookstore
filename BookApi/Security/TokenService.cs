using Bookstore.Api.Models.Entity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Api.Security
{
    // Названия claims в токене. Используем короткие стандартные имена JWT
    // ("sub", "name", "email", "role"), а не длинные URL из ClaimTypes.
    public static class AppClaims
    {
        public const string UserId = "sub";   // subject — кому выдан токен
        public const string Name = "name";
        public const string Email = "email";
        public const string Role = "role";
    }

    public interface ITokenService
    {
        // Создать токен для пользователя. Возвращает строку токена и время окончания.
        (string Token, DateTime ExpiresAt) CreateToken(User user);
    }

    // Создаёт подписанные JWT-токены.
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(JwtSettings settings)
        {
            _settings = settings;
        }

        public (string Token, DateTime ExpiresAt) CreateToken(User user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes);

            // SecurityTokenDescriptor — "описание" будущего токена
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                Expires = expiresAt,

                // Claims — что известно о пользователе. Хеш пароля сюда НЕ кладём:
                // данные токена не зашифрованы, их может прочитать любой (они только подписаны).
                Claims = new Dictionary<string, object>
                {
                    [AppClaims.UserId] = user.Id.ToString(),
                    [AppClaims.Name] = user.Name,
                    [AppClaims.Email] = user.Email,
                    [AppClaims.Role] = user.Role
                },

                // Подпись алгоритмом HMAC-SHA256 нашим секретным ключом
                SigningCredentials = new SigningCredentials(_settings.SigningKey, SecurityAlgorithms.HmacSha256)
            };

            var token = new JsonWebTokenHandler().CreateToken(descriptor);
            return (token, expiresAt);
        }
    }
}
