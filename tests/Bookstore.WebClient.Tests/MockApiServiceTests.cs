using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Mock;
using System.Net;

namespace Bookstore.WebClient.Tests
{
    // Мок-реализация API должна вести себя как настоящий API: те же коды, новые Id, копии данных.
    public class MockApiServiceTests
    {
        // Каждый тест получает своё свежее хранилище — тесты не мешают друг другу
        private readonly MockApiService<AuthorDto> _authors = new(new MockDataStore());

        [Fact]
        public async Task GetAll_ReturnsSeedData()
        {
            var response = await _authors.GetAllAsync();

            Assert.True(response.IsSuccess);
            Assert.Contains(response.Result!, a => a.Name == "Тарас Шевченко");
        }

        [Fact]
        public async Task Create_AssignsNewId_IgnoringClientId()
        {
            var response = await _authors.CreateAsync(new AuthorDto { Id = 999, Name = "Новий автор" });

            Assert.Equal(HttpStatusCode.Created, response.HttpStatusCode);
            Assert.NotEqual(999, response.Result!.Id);
            Assert.Equal("Новий автор", (await _authors.GetByIdAsync(response.Result.Id)).Result!.Name);
        }

        [Fact]
        public async Task ReturnedObjects_AreCopies()
        {
            // Если изменить полученный объект, "база" не должна измениться
            var first = (await _authors.GetByIdAsync(1)).Result!;
            first.Name = "Змінено ззовні";

            var again = (await _authors.GetByIdAsync(1)).Result!;
            Assert.Equal("Тарас Шевченко", again.Name);
        }

        [Fact]
        public async Task Update_Missing_Returns404()
        {
            var response = await _authors.UpdateAsync(12345, new AuthorDto { Name = "Хтось" });

            Assert.Equal(HttpStatusCode.NotFound, response.HttpStatusCode);
        }

        [Fact]
        public async Task Delete_RemovesRecord_SecondDeleteReturns404()
        {
            var first = await _authors.DeleteAsync(1);
            var second = await _authors.DeleteAsync(1);

            Assert.Equal(HttpStatusCode.NoContent, first.HttpStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, second.HttpStatusCode);
        }
    }
}
