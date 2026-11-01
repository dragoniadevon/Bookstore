using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Mock
{
    // Начальные мок-данные. Каждый метод возвращает НОВЫЙ список,
    // чтобы изменения в MockDataStore не портили "эталон".
    // Тексты — на украинском, как и весь интерфейс магазина.
    public static class MockSeedData
    {
        public static List<AuthorDto> Authors() =>
        [
            new() { Id = 1, Name = "Тарас Шевченко" },
            new() { Id = 2, Name = "Іван Франко" },
            new() { Id = 3, Name = "Леся Українка" },
            new() { Id = 4, Name = "Джордж Орвелл" },
            new() { Id = 5, Name = "Джейн Остін" },
            new() { Id = 6, Name = "Еріх Марія Ремарк" },
            new() { Id = 7, Name = "Антуан де Сент-Екзюпері" },
            new() { Id = 8, Name = "Габріель Гарсія Маркес" },
            new() { Id = 9, Name = "Рей Бредбері" },
            new() { Id = 10, Name = "Ліна Костенко" }
        ];

        public static List<GenreDto> Genres() =>
        [
            new() { Id = 1, Name = "Класика" },
            new() { Id = 2, Name = "Антиутопія" },
            new() { Id = 3, Name = "Роман" },
            new() { Id = 4, Name = "Казка" },
            new() { Id = 5, Name = "Фантастика" },
            new() { Id = 6, Name = "Поезія" }
        ];

        public static List<PublisherDto> Publishers() =>
        [
            new() { Id = 1, Name = "Книжковий дім", Description = "Класична та сучасна проза" },
            new() { Id = 2, Name = "Північне видавництво", Description = "Фантастика та зарубіжна література" }
        ];

        public static List<BookDto> Books() =>
        [
            new() { Id = 1, Title = "Кобзар", AuthorId = 1, GenreId = 6, PublisherId = 1, Price = 450, StockQuantity = 12,
                Description = "Головна книга української поезії: «Заповіт», «Катерина», «Сон» та інші твори." },
            new() { Id = 2, Title = "Захар Беркут", AuthorId = 2, GenreId = 1, PublisherId = 1, Price = 320, StockQuantity = 8,
                Description = "Історична повість про громаду Тухольщини, що стала на захист рідної землі." },
            new() { Id = 3, Title = "Лісова пісня", AuthorId = 3, GenreId = 1, PublisherId = 2, Price = 290, StockQuantity = 5,
                Description = "Драма-феєрія про кохання мавки Лукаша — світ волинського лісу й людської душі." },
            new() { Id = 4, Title = "1984", AuthorId = 4, GenreId = 2, PublisherId = 2, Price = 380, StockQuantity = 15,
                Description = "Світ тотального контролю, де Великий Брат стежить за кожним." },
            new() { Id = 5, Title = "Колгосп тварин", AuthorId = 4, GenreId = 2, PublisherId = 2, Price = 250, StockQuantity = 0,
                Description = "Сатирична притча про революцію, яка зрадила свої ідеали." },
            new() { Id = 6, Title = "Гордість і упередження", AuthorId = 5, GenreId = 3, PublisherId = 1, Price = 360, StockQuantity = 9,
                Description = "Дотепна історія Елізабет Беннет і містера Дарсі." },
            new() { Id = 7, Title = "Три товариші", AuthorId = 6, GenreId = 3, PublisherId = 1, Price = 410, StockQuantity = 7,
                Description = "Дружба, кохання й надія в Німеччині між двома війнами." },
            new() { Id = 8, Title = "Маленький принц", AuthorId = 7, GenreId = 4, PublisherId = 2, Price = 270, StockQuantity = 20,
                Description = "Мудра казка для дорослих про дружбу, відповідальність і троянду." },
            new() { Id = 9, Title = "Сто років самотності", AuthorId = 8, GenreId = 3, PublisherId = 1, Price = 480, StockQuantity = 4,
                Description = "Сага про родину Буендіа та містечко Макондо." },
            new() { Id = 10, Title = "451° за Фаренгейтом", AuthorId = 9, GenreId = 5, PublisherId = 2, Price = 340, StockQuantity = 11,
                Description = "Майбутнє, у якому пожежники не гасять пожежі, а палять книги." },
            new() { Id = 11, Title = "Маруся Чурай", AuthorId = 10, GenreId = 6, PublisherId = 1, Price = 390, StockQuantity = 3,
                Description = "Історичний роман у віршах про легендарну піснярку з Полтави." },
            new() { Id = 12, Title = "Марсіанські хроніки", AuthorId = 9, GenreId = 5, PublisherId = 2, Price = 350, StockQuantity = 6,
                Description = "Поетичні оповідання про колонізацію Марса." }
        ];
    }
}
