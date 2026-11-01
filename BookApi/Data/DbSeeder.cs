using Bookstore.Api.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Data
{
    // Заполняет базу демонстрационным каталогом книг.
    //
    // Вызывается из Program.cs при запуске API в режиме разработки (Development).
    // Добавляет данные, только если в базе ещё нет ни одной книги, поэтому
    // повторный запуск ничего не дублирует и не трогает уже добавленные книги.
    //
    // Почему не HasData в AppDbContext? HasData требует фиксированных Id
    // и превращает каждое изменение каталога в новую миграцию. Здесь же Id
    // выдаёт база, а авторы/жанры/издательства ищутся по имени: если такой уже
    // есть (например, "Тарас Шевченко" из HasData), он используется повторно.
    //
    // Каталог совпадает с мок-данными WebClient (Service/Mock/MockSeedData.cs).
    internal static class DbSeeder
    {
        public static async Task SeedDemoCatalogAsync(AppDbContext db)
        {
            // AnyAsync — есть ли хотя бы одна книга. Если есть — ничего не делаем.
            if (await db.Books.AnyAsync())
            {
                return;
            }

            var authors = await GetOrCreateAsync(db.Authors, a => a.Name, name => new Author { Name = name },
                "Тарас Шевченко", "Іван Франко", "Леся Українка", "Джордж Орвелл", "Джейн Остін",
                "Еріх Марія Ремарк", "Антуан де Сент-Екзюпері", "Габріель Гарсія Маркес", "Рей Бредбері", "Ліна Костенко");

            var genres = await GetOrCreateAsync(db.Genres, g => g.Name, name => new Genre { Name = name },
                "Класика", "Антиутопія", "Роман", "Казка", "Фантастика", "Поезія");

            var publishers = await GetOrCreateAsync(db.Publishers, p => p.Name, name => new Publisher { Name = name },
                "Книжковий дім", "Північне видавництво");
            publishers["Книжковий дім"].Description ??= "Класична та сучасна проза";
            publishers["Північне видавництво"].Description ??= "Фантастика та зарубіжна література";

            // Локальная функция: короткая запись создания книги.
            // Связи задаём через навигационные свойства (Author = ...),
            // EF Core сам подставит AuthorId/GenreId/PublisherId при сохранении.
            Book NewBook(string title, string author, string genre, string publisher, decimal price, int stock, string description) => new()
            {
                Title = title,
                Author = authors[author],
                Genre = genres[genre],
                Publisher = publishers[publisher],
                Price = price,
                StockQuantity = stock,
                Description = description
            };

            db.Books.AddRange(
                NewBook("Кобзар", "Тарас Шевченко", "Поезія", "Книжковий дім", 450, 12,
                    "Головна книга української поезії: «Заповіт», «Катерина», «Сон» та інші твори."),
                NewBook("Захар Беркут", "Іван Франко", "Класика", "Книжковий дім", 320, 8,
                    "Історична повість про громаду Тухольщини, що стала на захист рідної землі."),
                NewBook("Лісова пісня", "Леся Українка", "Класика", "Північне видавництво", 290, 5,
                    "Драма-феєрія про кохання мавки Лукаша — світ волинського лісу й людської душі."),
                NewBook("1984", "Джордж Орвелл", "Антиутопія", "Північне видавництво", 380, 15,
                    "Світ тотального контролю, де Великий Брат стежить за кожним."),
                NewBook("Колгосп тварин", "Джордж Орвелл", "Антиутопія", "Північне видавництво", 250, 0,
                    "Сатирична притча про революцію, яка зрадила свої ідеали."),
                NewBook("Гордість і упередження", "Джейн Остін", "Роман", "Книжковий дім", 360, 9,
                    "Дотепна історія Елізабет Беннет і містера Дарсі."),
                NewBook("Три товариші", "Еріх Марія Ремарк", "Роман", "Книжковий дім", 410, 7,
                    "Дружба, кохання й надія в Німеччині між двома війнами."),
                NewBook("Маленький принц", "Антуан де Сент-Екзюпері", "Казка", "Північне видавництво", 270, 20,
                    "Мудра казка для дорослих про дружбу, відповідальність і троянду."),
                NewBook("Сто років самотності", "Габріель Гарсія Маркес", "Роман", "Книжковий дім", 480, 4,
                    "Сага про родину Буендіа та містечко Макондо."),
                NewBook("451° за Фаренгейтом", "Рей Бредбері", "Фантастика", "Північне видавництво", 340, 11,
                    "Майбутнє, у якому пожежники не гасять пожежі, а палять книги."),
                NewBook("Маруся Чурай", "Ліна Костенко", "Поезія", "Книжковий дім", 390, 3,
                    "Історичний роман у віршах про легендарну піснярку з Полтави."),
                NewBook("Марсіанські хроніки", "Рей Бредбері", "Фантастика", "Північне видавництво", 350, 6,
                    "Поетичні оповідання про колонізацію Марса."));

            // Один SaveChangesAsync сохраняет всё сразу (в одной транзакции).
            await db.SaveChangesAsync();
        }

        // Находит в таблице записи с нужными именами, а недостающие создаёт.
        // Возвращает словарь "имя -> объект", чтобы по имени получить автора/жанр.
        //
        // <T> — Author, Genre или Publisher.
        // Func<T, string> getName — функция, которая достаёт имя из объекта (a => a.Name).
        // Func<string, T> create — функция, которая создаёт новый объект по имени
        // (name => new Author { Name = name }).
        private static async Task<Dictionary<string, T>> GetOrCreateAsync<T>(
            DbSet<T> table, Func<T, string> getName, Func<string, T> create, params string[] names)
            where T : class
        {
            var existing = await table.ToListAsync();
            var result = new Dictionary<string, T>();

            foreach (var name in names)
            {
                var item = existing.FirstOrDefault(x => getName(x) == name);
                if (item == null)
                {
                    item = create(name);
                    table.Add(item);
                }

                result[name] = item;
            }

            return result;
        }
    }
}
