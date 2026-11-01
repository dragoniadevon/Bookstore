using Bookstore.Api.Models.DTO;
using Bookstore.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Api.Tests
{
    // Каталог: кто может читать и изменять, удаление со связями, PATCH
    public class CatalogTests : ApiTestBase
    {
        public CatalogTests(ApiFactory factory) : base(factory) { }

        private static BookDTO NewBook() => new()
        {
            Title = "Нова книга",
            Price = 199,
            StockQuantity = 3,
            AuthorId = 1,
            GenreId = 1,
            PublisherId = 1
        };

        [Fact]
        public async Task GetBooks_Anonymous_Returns200()
        {
            var response = await Anonymous.GetAsync("api/Book");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateBook_Anonymous_Returns401()
        {
            var response = await Anonymous.PostAsJsonAsync("api/Book", NewBook());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateBook_AsCustomer_Returns403()
        {
            // Покупатель вошёл, но роли Admin у него нет -> 403 Forbidden
            var (customer, _) = await RegisterCustomerAsync();

            var response = await customer.PostAsJsonAsync("api/Book", NewBook());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateBook_AsAdmin_Returns201WithLocation()
        {
            var admin = await CreateAdminClientAsync();

            var response = await admin.PostAsJsonAsync("api/Book", NewBook());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            var created = await response.Content.ReadFromJsonAsync<BookDTO>();
            Assert.True(created!.Id > 0);
        }

        [Fact]
        public async Task CreateAuthor_IgnoresClientId()
        {
            var admin = await CreateAdminClientAsync();

            var response = await admin.PostAsJsonAsync("api/Author", new AuthorDTO { Id = 9999, Name = "Новий автор" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<AuthorDTO>();
            Assert.NotEqual(9999, created!.Id);
        }

        [Fact]
        public async Task DeleteAuthorWithBooks_Returns409()
        {
            // Arrange: у автора с Id = 1 точно есть книга
            await CreateBookAsync(stock: 1);
            var admin = await CreateAdminClientAsync();

            // Act
            var response = await admin.DeleteAsync("api/Author/1");

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMissingAuthor_Returns404()
        {
            var admin = await CreateAdminClientAsync();

            var response = await admin.DeleteAsync("api/Author/987654");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PatchBook_OnlyStockZero_ChangesOnlyStock()
        {
            var book = await CreateBookAsync(stock: 5, price: 321);
            var admin = await CreateAdminClientAsync();

            var response = await admin.PatchAsJsonAsync($"api/Book/{book.Id}", new BookPatchDTO { StockQuantity = 0 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await GetBookAsync(book.Id);
            Assert.Equal(0, updated.StockQuantity);
            Assert.Equal(321, updated.Price);        // остальные поля не изменились
            Assert.Equal(book.Title, updated.Title);
        }
    }
}
