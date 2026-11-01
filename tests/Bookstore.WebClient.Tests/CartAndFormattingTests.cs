using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Models.Api;
using Bookstore.WebClient.Service;
using Bookstore.WebClient.Tests.TestDoubles;

namespace Bookstore.WebClient.Tests
{
    // Корзина в сессии
    public class SessionCartServiceTests
    {
        private readonly SessionCartService _cart = new(TestSession.CreateAccessor());

        private static BookCard Book(int id, int stock, decimal price = 100) => new()
        {
            Id = id,
            Title = $"Книга {id}",
            AuthorName = "Автор",
            Price = price,
            StockQuantity = stock
        };

        [Fact]
        public async Task Add_SameBookTwice_SumsQuantity()
        {
            await _cart.AddAsync(Book(1, stock: 10), 2);
            await _cart.AddAsync(Book(1, stock: 10), 3);

            var item = Assert.Single(await _cart.GetItemsAsync());
            Assert.Equal(5, item.Quantity);
            Assert.Equal(5, await _cart.CountAsync());
        }

        [Fact]
        public async Task Add_MoreThanStock_CapsQuantityAndReturnsError()
        {
            var error = await _cart.AddAsync(Book(1, stock: 2), 5);

            Assert.NotNull(error);
            Assert.Equal(2, Assert.Single(await _cart.GetItemsAsync()).Quantity);
        }

        [Fact]
        public async Task Add_OutOfStock_ReturnsErrorAndAddsNothing()
        {
            var error = await _cart.AddAsync(Book(1, stock: 0));

            Assert.NotNull(error);
            Assert.Empty(await _cart.GetItemsAsync());
        }

        [Fact]
        public async Task SetQuantity_Zero_RemovesItem()
        {
            await _cart.AddAsync(Book(1, stock: 5));

            await _cart.SetQuantityAsync(1, 0);

            Assert.Empty(await _cart.GetItemsAsync());
        }

        [Fact]
        public async Task Clear_RemovesEverything()
        {
            await _cart.AddAsync(Book(1, stock: 5));
            await _cart.AddAsync(Book(2, stock: 5));

            await _cart.ClearAsync();

            Assert.Equal(0, await _cart.CountAsync());
        }
    }

    // Преобразование ответов API в модели клиента
    public class ApiMappingTests
    {
        [Fact]
        public void CartFromApi_MapsStockToMaxQuantity()
        {
            var dto = new ApiCartDto
            {
                Items = [new ApiCartItemDto { BookId = 7, Title = "Кобзар", Price = 450, Quantity = 2, StockQuantity = 12 }]
            };

            var item = Assert.Single(ApiCartService.ToItems(dto));

            Assert.Equal(7, item.BookId);
            Assert.Equal(12, item.MaxQuantity);
            Assert.Equal(900, item.Total);
        }

        [Fact]
        public void OrderFromApi_UsesUnitPriceAsItemPrice()
        {
            var dto = new ApiOrderDto
            {
                Number = "BS-1",
                TotalAmount = 300,
                Items = [new ApiOrderItemDto { BookId = 1, Title = "1984", Quantity = 3, UnitPrice = 100 }]
            };

            var order = ApiOrderService.ToOrder(dto);

            Assert.Equal("BS-1", order.Number);
            Assert.Equal(300, order.Total);
            Assert.Equal(100, Assert.Single(order.Items).Price);
        }
    }

    // Форматирование цен и окончаний
    public class FormattingTests
    {
        [Theory]
        [InlineData(450, "450 ₴")]
        [InlineData(1290.5, "1 290,5 ₴")]
        [InlineData(199.99, "199,99 ₴")]
        public void ToUah_FormatsHryvnia(decimal value, string expected)
        {
            // Украинский формат разделяет тысячи неразрывным пробелом (U+00A0);
            // для сравнения заменяем его обычным
            Assert.Equal(expected, value.ToUah().Replace(' ', ' '));
        }

        [Theory]
        [InlineData(1, "1 книга")]
        [InlineData(2, "2 книги")]
        [InlineData(5, "5 книг")]
        [InlineData(11, "11 книг")]
        [InlineData(21, "21 книга")]
        [InlineData(24, "24 книги")]
        public void Plural_ChoosesCorrectForm(int count, string expected)
        {
            Assert.Equal(expected, count.Plural("книга", "книги", "книг"));
        }
    }
}
