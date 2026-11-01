using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Mock
{
    // "База данных" в памяти для мок-режима.
    //
    // Регистрируется как Singleton: одна на всё приложение, поэтому книги,
    // добавленные в админке, видны всем посетителям. При перезапуске приложения
    // всё возвращается к начальным данным из MockSeedData.
    public class MockDataStore
    {
        // Таблицы: тип DTO -> список записей
        private readonly Dictionary<Type, object> _tables = new()
        {
            [typeof(AuthorDto)] = MockSeedData.Authors(),
            [typeof(GenreDto)] = MockSeedData.Genres(),
            [typeof(PublisherDto)] = MockSeedData.Publishers(),
            [typeof(BookDto)] = MockSeedData.Books()
        };

        // Объект для lock: запросы приходят одновременно из разных потоков,
        // и без блокировки два запроса могли бы испортить список.
        public object SyncRoot { get; } = new();

        // Получить "таблицу" для нужного типа, например Table<BookDto>()
        public List<T> Table<T>() where T : class, IEntityDto =>
            _tables.TryGetValue(typeof(T), out var table)
                ? (List<T>)table
                : throw new InvalidOperationException($"В мок-хранилище нет таблицы для {typeof(T).Name}");
    }
}
