using Bookstore.Api.Data;
using Bookstore.Api.Models;
using Bookstore.Api.Models.Entity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Bookstore.Api.Tests.Infrastructure
{
    // Запускает настоящий Bookstore.Api прямо в памяти теста.
    //
    // WebApplicationFactory<Program> берёт Program.cs из API, выполняет все регистрации
    // и даёт HttpClient, который отправляет запросы в это приложение без сети.
    // Мы проверяем всю цепочку: Controller -> Service -> Repository -> база.
    //
    // Отличия от обычного запуска:
    // - вместо SQL Server — SQLite в памяти (быстро, ничего не нужно устанавливать,
    //   после теста база исчезает);
    // - окружение "Testing" (демо-данные из DbSeeder не добавляются);
    // - фиксированный тестовый ключ JWT.
    public class ApiFactory : WebApplicationFactory<Program>
    {
        public const string AdminEmail = "admin@test.local";
        public const string AdminPassword = "admin123";

        // База SQLite ":memory:" живёт, пока открыто соединение,
        // поэтому держим одно соединение на всё время жизни фабрики.
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "test-signing-key-for-integration-tests-0123456789"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Убираем регистрацию SQL Server из Program.cs...
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                // ...и подключаем SQLite в памяти
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            });
        }

        // Вызывается один раз при создании приложения: создаём таблицы и тестовые данные.
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // EnsureCreated создаёт таблицы по модели (миграции написаны под SQL Server,
            // для SQLite используем модель напрямую). HasData из AppDbContext тоже
            // добавится: автор, жанр и издательство с Id = 1.
            db.Database.EnsureCreated();

            var hasher = new PasswordHasher<User>();
            var admin = new User { Name = "Test Admin", Email = AdminEmail, Role = UserRoles.Admin };
            admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);
            db.Users.Add(admin);
            db.SaveChanges();

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
