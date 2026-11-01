using Bookstore.Api.Data;
using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Security;
using Bookstore.Api.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Services
{
    // Регистрация и вход пользователей.
    //
    // Работает с AppDbContext напрямую: операций с пользователями немного
    // и они специфичные (поиск по email), общий IBaseRepository тут не подходит.
    internal class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly ITokenService _tokens;

        // PasswordHasher из ASP.NET Core Identity: делает хеш пароля с "солью"
        // (случайной добавкой), поэтому у двух одинаковых паролей разные хеши.
        private readonly PasswordHasher<User> _hasher = new();

        public AuthService(AppDbContext db, ITokenService tokens)
        {
            _db = db;
            _tokens = tokens;
        }

        public async Task<ServiceResult<AuthResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            // Email сравниваем без учёта регистра и лишних пробелов
            var email = NormalizeEmail(dto.Email);

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return ServiceResult<AuthResponseDTO>.Fail(ServiceStatus.Conflict, "Користувач із таким email уже зареєстрований.");
            }

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Role = UserRoles.Customer   // через регистрацию админом стать нельзя
            };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // После регистрации сразу выдаём токен, чтобы не нужно было входить ещё раз
            return ServiceResult<AuthResponseDTO>.Ok(CreateResponse(user));
        }

        public async Task<ServiceResult<AuthResponseDTO>> LoginAsync(LoginDTO dto)
        {
            var email = NormalizeEmail(dto.Email);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Одна и та же ошибка для "нет такого email" и "неверный пароль" —
            // чтобы нельзя было подбором узнать, какие email зарегистрированы.
            if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            {
                return ServiceResult<AuthResponseDTO>.Fail(ServiceStatus.Unauthorized, "Неправильний email або пароль.");
            }

            return ServiceResult<AuthResponseDTO>.Ok(CreateResponse(user));
        }

        public async Task<UserDTO?> GetUserAsync(int userId)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return user == null ? null : ToDto(user);
        }

        private AuthResponseDTO CreateResponse(User user)
        {
            var (token, expiresAt) = _tokens.CreateToken(user);
            return new AuthResponseDTO { Token = token, ExpiresAt = expiresAt, User = ToDto(user) };
        }

        private static UserDTO ToDto(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };

        internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
