using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Bookstore.Api.Security
{
    // Настройки JWT-токенов.
    //
    // JWT (JSON Web Token) — строка из трёх частей "заголовок.данные.подпись".
    // В "данных" лежат claims (кто пользователь, его роль, до когда действует токен),
    // а подпись сделана секретным ключом. Сервер по подписи проверяет, что токен
    // выдал именно он и что никто не изменил данные (например, роль на Admin).
    //
    // Ключ — секрет. В репозиторий его класть нельзя, он берётся из настройки "Jwt:Key"
    // (user-secrets, переменная окружения Jwt__Key и т.п.). Длина — минимум 32 символа.
    public class JwtSettings
    {
        // Кто выдал токен и для кого он предназначен — проверяются при входящем запросе
        public string Issuer { get; init; } = "Bookstore.Api";
        public string Audience { get; init; } = "Bookstore.Client";

        // Сколько минут действует токен
        public int ExpiresMinutes { get; init; } = 120;

        // Ключ, которым подписываются и проверяются токены
        public SymmetricSecurityKey SigningKey { get; init; } = null!;

        // true — ключ не задан в настройках и создан случайно при запуске
        public bool IsTemporaryKey { get; init; }

        // Читает настройки из конфигурации.
        //
        // Если ключа нет:
        // - в режиме Development/Testing создаём случайный ключ (токены перестанут
        //   действовать после перезапуска API — для разработки это нормально);
        // - на рабочем сервере (Production) — ошибка: без настоящего ключа запускаться нельзя.
        public static JwtSettings FromConfiguration(IConfiguration configuration, IHostEnvironment environment)
        {
            var section = configuration.GetSection("Jwt");
            var key = section["Key"];
            var isTemporary = false;

            if (string.IsNullOrWhiteSpace(key))
            {
                if (environment.IsProduction())
                {
                    throw new InvalidOperationException(
                        "Не задан ключ JWT. Укажите настройку Jwt:Key (не короче 32 символов).");
                }

                // RandomNumberGenerator — криптографически стойкий генератор случайных чисел
                key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                isTemporary = true;
            }

            if (Encoding.UTF8.GetByteCount(key) < 32)
            {
                throw new InvalidOperationException("Ключ JWT (Jwt:Key) должен быть не короче 32 символов.");
            }

            return new JwtSettings
            {
                Issuer = section["Issuer"] ?? "Bookstore.Api",
                Audience = section["Audience"] ?? "Bookstore.Client",
                ExpiresMinutes = int.TryParse(section["ExpiresMinutes"], out var minutes) ? minutes : 120,
                SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                IsTemporaryKey = isTemporary
            };
        }
    }
}
