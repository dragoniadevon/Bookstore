using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Api.Tests
{
    // Регистрация, вход и проверка токена: /api/Auth
    //
    // Каждый тест построен по схеме Arrange / Act / Assert:
    // - Arrange — подготовить данные;
    // - Act — выполнить проверяемое действие;
    // - Assert — проверить результат.
    public class AuthTests : ApiTestBase
    {
        public AuthTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Register_ReturnsTokenAndCustomerRole()
        {
            // Act
            var (_, auth) = await RegisterCustomerAsync();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(auth.Token));
            Assert.Equal(UserRoles.Customer, auth.User.Role);
            Assert.True(auth.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409()
        {
            // Arrange: такой email уже есть (администратор из ApiFactory)
            var dto = new RegisterDTO { Name = "Хтось", Email = ApiFactory.AdminEmail, Password = "secret123" };

            // Act
            var response = await Anonymous.PostAsJsonAsync("api/Auth/register", dto);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_EmailIsCaseInsensitive()
        {
            var dto = new RegisterDTO { Name = "Хтось", Email = ApiFactory.AdminEmail.ToUpperInvariant(), Password = "secret123" };

            var response = await Anonymous.PostAsJsonAsync("api/Auth/register", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_ShortPassword_Returns400()
        {
            var dto = new RegisterDTO { Name = "Хтось", Email = "short@test.local", Password = "123" };

            var response = await Anonymous.PostAsJsonAsync("api/Auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var response = await Anonymous.PostAsJsonAsync("api/Auth/login",
                new LoginDTO { Email = ApiFactory.AdminEmail, Password = "wrong-password" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_UnknownEmail_Returns401()
        {
            var response = await Anonymous.PostAsJsonAsync("api/Auth/login",
                new LoginDTO { Email = "nobody@test.local", Password = "whatever" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_WithoutToken_Returns401()
        {
            var response = await Anonymous.GetAsync("api/Auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_WithToken_ReturnsCurrentUser()
        {
            var (client, auth) = await RegisterCustomerAsync();

            var me = await client.GetFromJsonAsync<UserDTO>("api/Auth/me");

            Assert.NotNull(me);
            Assert.Equal(auth.User.Email, me.Email);
        }

        [Fact]
        public async Task Me_WithForgedToken_Returns401()
        {
            // Токен с изменённой подписью: сервер должен его отклонить
            var (_, auth) = await RegisterCustomerAsync();
            var forged = auth.Token[..^4] + "abcd";

            var response = await CreateClient(forged).GetAsync("api/Auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
