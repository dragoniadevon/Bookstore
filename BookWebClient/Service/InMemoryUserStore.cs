using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;

namespace Bookstore.WebClient.Service
{
    // Демо-хранилище пользователей в памяти. При перезапуске приложения
    // зарегистрированные пользователи пропадают, остаются только два встроенных:
    //   demo@bookstore.local  / demo123  — покупатель
    //   admin@bookstore.local / admin123 — администратор (может управлять книгами)
    //
    // Регистрируется как Singleton: один объект на всё приложение,
    // иначе пользователи "терялись" бы между запросами.
    public class InMemoryUserStore : IUserStore
    {
        // ConcurrentDictionary — словарь, безопасный при одновременном доступе
        // из нескольких запросов (обычный Dictionary в такой ситуации может сломаться).
        private readonly ConcurrentDictionary<string, ShopUser> _users = new(StringComparer.OrdinalIgnoreCase);

        // PasswordHasher из ASP.NET Core Identity: делает хеш пароля с "солью"
        // и умеет проверять пароль по хешу.
        private readonly PasswordHasher<ShopUser> _hasher = new();

        public InMemoryUserStore()
        {
            Register("Демо Покупець", "demo@bookstore.local", "demo123");
            var admin = Register("Адміністратор", "admin@bookstore.local", "admin123");
            if (admin != null) admin.Role = ShopRoles.Admin;
        }

        public ShopUser? Register(string name, string email, string password)
        {
            var user = new ShopUser { Name = name.Trim(), Email = email.Trim() };
            user.PasswordHash = _hasher.HashPassword(user, password);

            // TryAdd вернёт false, если такой email уже есть
            return _users.TryAdd(user.Email, user) ? user : null;
        }

        public ShopUser? ValidateCredentials(string email, string password)
        {
            if (!_users.TryGetValue(email.Trim(), out var user)) return null;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Failed ? null : user;
        }
    }
}
