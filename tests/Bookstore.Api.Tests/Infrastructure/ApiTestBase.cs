using Bookstore.Api.Models.DTO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bookstore.Api.Tests.Infrastructure
{
    // Базовый класс для тестов API: общие помощники.
    //
    // IClassFixture<ApiFactory> — xUnit создаёт ОДНУ фабрику (одно приложение и одну
    // базу) на тестовый класс и передаёт её в конструктор. Тесты внутри класса
    // используют общую базу, поэтому каждый тест создаёт себе свои данные
    // (своего пользователя, свою книгу) и не зависит от других тестов.
    public abstract class ApiTestBase : IClassFixture<ApiFactory>
    {
        protected readonly ApiFactory Factory;

        // Клиент без токена — "гость"
        protected readonly HttpClient Anonymous;

        protected ApiTestBase(ApiFactory factory)
        {
            Factory = factory;
            Anonymous = factory.CreateClient();
        }

        // Клиент, который добавляет токен ко всем запросам
        protected HttpClient CreateClient(string token)
        {
            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected async Task<AuthResponseDTO> LoginAsync(string email, string password)
        {
            var response = await Anonymous.PostAsJsonAsync("api/Auth/login", new LoginDTO { Email = email, Password = password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<AuthResponseDTO>())!;
        }

        protected async Task<HttpClient> CreateAdminClientAsync()
        {
            var auth = await LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
            return CreateClient(auth.Token);
        }

        // Зарегистрировать нового покупателя с уникальным email
        protected async Task<(HttpClient Client, AuthResponseDTO Auth)> RegisterCustomerAsync()
        {
            var dto = new RegisterDTO
            {
                Name = "Покупець",
                Email = $"user-{Guid.NewGuid():N}@test.local",
                Password = "secret123"
            };

            var response = await Anonymous.PostAsJsonAsync("api/Auth/register", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDTO>())!;
            return (CreateClient(auth.Token), auth);
        }

        // Создать книгу от имени администратора (автор, жанр, издательство с Id = 1 — из HasData)
        protected async Task<BookDTO> CreateBookAsync(int stock, decimal price = 100m)
        {
            var admin = await CreateAdminClientAsync();
            var response = await admin.PostAsJsonAsync("api/Book", new BookDTO
            {
                Title = $"Тестова книга {Guid.NewGuid():N}",
                Price = price,
                StockQuantity = stock,
                AuthorId = 1,
                GenreId = 1,
                PublisherId = 1
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<BookDTO>())!;
        }

        protected async Task<BookDTO> GetBookAsync(int id) =>
            (await Anonymous.GetFromJsonAsync<BookDTO>($"api/Book/{id}"))!;
    }
}
