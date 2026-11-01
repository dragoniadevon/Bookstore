using Bookstore.Api.Models.DTO;
using Bookstore.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Api.Tests
{
    // Оформление заказов: /api/Order
    public class OrderTests : ApiTestBase
    {
        public OrderTests(ApiFactory factory) : base(factory) { }

        private static CreateOrderDTO Checkout() => new()
        {
            CustomerName = "Іван Петренко",
            Phone = "+380670000000",
            Address = "Київ, вул. Тестова, 1",
            CardLast4 = "4242",
            TransactionId = "test-transaction"
        };

        // Покупатель с книгой в корзине
        private async Task<(HttpClient Client, BookDTO Book)> CustomerWithBookInCartAsync(int stock, int quantity, decimal price = 100)
        {
            var book = await CreateBookAsync(stock, price);
            var (client, _) = await RegisterCustomerAsync();
            var add = await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id, Quantity = quantity });
            Assert.Equal(HttpStatusCode.OK, add.StatusCode);
            return (client, book);
        }

        [Fact]
        public async Task PlaceOrder_DecrementsStock_ClearsCart_ReturnsOrder()
        {
            // Arrange: на складе 5, покупаем 2 по 120
            var (client, book) = await CustomerWithBookInCartAsync(stock: 5, quantity: 2, price: 120);

            // Act
            var response = await client.PostAsJsonAsync("api/Order", Checkout());

            // Assert: заказ создан
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderDTO>();
            Assert.StartsWith("BS-", order!.Number);
            Assert.Equal(240, order.TotalAmount);
            var item = Assert.Single(order.Items);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(120, item.UnitPrice);

            // Assert: остаток уменьшился на 2
            Assert.Equal(3, (await GetBookAsync(book.Id)).StockQuantity);

            // Assert: корзина очищена
            var cart = await client.GetFromJsonAsync<CartDTO>("api/Cart");
            Assert.Empty(cart!.Items);
        }

        [Fact]
        public async Task PlaceOrder_EmptyCart_Returns400()
        {
            var (client, _) = await RegisterCustomerAsync();

            var response = await client.PostAsJsonAsync("api/Order", Checkout());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PlaceOrder_StockDroppedAfterAddingToCart_Returns409_AndChangesNothing()
        {
            // Arrange: покупатель положил 2 книги, а потом админ уменьшил остаток до 1
            var (client, book) = await CustomerWithBookInCartAsync(stock: 5, quantity: 2);
            var admin = await CreateAdminClientAsync();
            await admin.PatchAsJsonAsync($"api/Book/{book.Id}", new BookPatchDTO { StockQuantity = 1 });

            // Act
            var response = await client.PostAsJsonAsync("api/Order", Checkout());

            // Assert: заказ не создан, остаток не изменился, корзина осталась
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(1, (await GetBookAsync(book.Id)).StockQuantity);
            var cart = await client.GetFromJsonAsync<CartDTO>("api/Cart");
            Assert.Single(cart!.Items);
            var orders = await client.GetFromJsonAsync<List<OrderDTO>>("api/Order");
            Assert.Empty(orders!);
        }

        [Fact]
        public async Task PriceChangeAfterOrder_DoesNotChangeOrderHistory()
        {
            var (client, book) = await CustomerWithBookInCartAsync(stock: 5, quantity: 1, price: 100);
            var placed = await (await client.PostAsJsonAsync("api/Order", Checkout())).Content.ReadFromJsonAsync<OrderDTO>();

            var admin = await CreateAdminClientAsync();
            await admin.PatchAsJsonAsync($"api/Book/{book.Id}", new BookPatchDTO { Price = 999 });

            var order = await client.GetFromJsonAsync<OrderDTO>($"api/Order/{placed!.Number}");
            Assert.Equal(100, order!.TotalAmount);
            Assert.Equal(100, order.Items[0].UnitPrice);
        }

        [Fact]
        public async Task GetOrders_ReturnsOnlyOwnOrders()
        {
            var (first, _) = await CustomerWithBookInCartAsync(stock: 5, quantity: 1);
            await first.PostAsJsonAsync("api/Order", Checkout());
            var (second, _) = await RegisterCustomerAsync();

            var firstOrders = await first.GetFromJsonAsync<List<OrderDTO>>("api/Order");
            var secondOrders = await second.GetFromJsonAsync<List<OrderDTO>>("api/Order");

            Assert.Single(firstOrders!);
            Assert.Empty(secondOrders!);
        }

        [Fact]
        public async Task GetOrderOfAnotherUser_Returns404()
        {
            var (owner, _) = await CustomerWithBookInCartAsync(stock: 5, quantity: 1);
            var placed = await (await owner.PostAsJsonAsync("api/Order", Checkout())).Content.ReadFromJsonAsync<OrderDTO>();
            var (stranger, _) = await RegisterCustomerAsync();

            var response = await stranger.GetAsync($"api/Order/{placed!.Number}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBookThatWasOrdered_Returns409()
        {
            var (client, book) = await CustomerWithBookInCartAsync(stock: 5, quantity: 1);
            await client.PostAsJsonAsync("api/Order", Checkout());
            var admin = await CreateAdminClientAsync();

            var response = await admin.DeleteAsync($"api/Book/{book.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task PlaceOrder_WithoutToken_Returns401()
        {
            var response = await Anonymous.PostAsJsonAsync("api/Order", Checkout());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
