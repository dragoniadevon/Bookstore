using Bookstore.Api.Models.DTO;
using Bookstore.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Api.Tests
{
    // Корзина: /api/Cart
    public class CartTests : ApiTestBase
    {
        public CartTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Cart_WithoutToken_Returns401()
        {
            var response = await Anonymous.GetAsync("api/Cart");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task NewUser_HasEmptyCart()
        {
            var (client, _) = await RegisterCustomerAsync();

            var cart = await client.GetFromJsonAsync<CartDTO>("api/Cart");

            Assert.Empty(cart!.Items);
            Assert.Equal(0, cart.TotalAmount);
        }

        [Fact]
        public async Task AddItem_ThenGet_ReturnsItemWithTotals()
        {
            // Arrange
            var book = await CreateBookAsync(stock: 5, price: 150);
            var (client, _) = await RegisterCustomerAsync();

            // Act: добавляем 2 штуки, потом ещё 1 — количество должно сложиться
            await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id, Quantity = 2 });
            var response = await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id, Quantity = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cart = await client.GetFromJsonAsync<CartDTO>("api/Cart");
            var item = Assert.Single(cart!.Items);
            Assert.Equal(3, item.Quantity);
            Assert.Equal(450, item.Total);
            Assert.Equal(3, cart.TotalQuantity);
            Assert.Equal(450, cart.TotalAmount);
        }

        [Fact]
        public async Task AddItem_MoreThanStock_Returns409()
        {
            var book = await CreateBookAsync(stock: 2);
            var (client, _) = await RegisterCustomerAsync();

            var response = await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id, Quantity = 3 });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task AddItem_UnknownBook_Returns404()
        {
            var (client, _) = await RegisterCustomerAsync();

            var response = await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = 987654 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SetQuantity_Zero_RemovesItem()
        {
            var book = await CreateBookAsync(stock: 5);
            var (client, _) = await RegisterCustomerAsync();
            await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id });

            var response = await client.PutAsJsonAsync($"api/Cart/items/{book.Id}", new UpdateCartItemDTO { Quantity = 0 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cart = await client.GetFromJsonAsync<CartDTO>("api/Cart");
            Assert.Empty(cart!.Items);
        }

        [Fact]
        public async Task SetQuantity_Negative_Returns400()
        {
            var book = await CreateBookAsync(stock: 5);
            var (client, _) = await RegisterCustomerAsync();
            await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id });

            var response = await client.PutAsJsonAsync($"api/Cart/items/{book.Id}", new UpdateCartItemDTO { Quantity = -1 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // [Range(0, 1000)] в UpdateCartItemDTO
        }

        [Fact]
        public async Task Clear_EmptiesCart()
        {
            var book = await CreateBookAsync(stock: 5);
            var (client, _) = await RegisterCustomerAsync();
            await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id });

            var response = await client.DeleteAsync("api/Cart");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var cart = await client.GetFromJsonAsync<CartDTO>("api/Cart");
            Assert.Empty(cart!.Items);
        }

        [Fact]
        public async Task Carts_AreSeparatedBetweenUsers()
        {
            var book = await CreateBookAsync(stock: 5);
            var (first, _) = await RegisterCustomerAsync();
            var (second, _) = await RegisterCustomerAsync();

            await first.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id });

            var secondCart = await second.GetFromJsonAsync<CartDTO>("api/Cart");
            Assert.Empty(secondCart!.Items);
        }
    }
}
