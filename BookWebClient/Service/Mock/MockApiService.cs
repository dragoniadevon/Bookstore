using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using System.Net;
using System.Text.Json;

namespace Bookstore.WebClient.Service.Mock
{
    // Мок-реализация IBaseApiService: делает то же, что и API, но с данными в памяти.
    //
    // Отвечает теми же кодами, что и настоящий API (200, 201, 204, 404),
    // поэтому страницам всё равно, с каким источником они работают.
    public class MockApiService<T> : IBaseApiService<T> where T : class, IEntityDto
    {
        private readonly MockDataStore _store;

        public MockApiService(MockDataStore store)
        {
            _store = store;
        }

        private List<T> Table => _store.Table<T>();

        public Task<ApiResponse<List<T>>> GetAllAsync(CancellationToken ct = default)
        {
            lock (_store.SyncRoot)
            {
                return Ok(Table.Select(Clone).ToList());
            }
        }

        public Task<ApiResponse<T>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            lock (_store.SyncRoot)
            {
                var item = Table.FirstOrDefault(x => x.Id == id);
                return item == null ? NotFound<T>() : Ok(Clone(item));
            }
        }

        public Task<ApiResponse<T>> CreateAsync(T entity, CancellationToken ct = default)
        {
            lock (_store.SyncRoot)
            {
                // Id выдаёт "база": следующий после самого большого
                var copy = Clone(entity);
                copy.Id = Table.Count == 0 ? 1 : Table.Max(x => x.Id) + 1;
                Table.Add(copy);
                return Ok(Clone(copy), HttpStatusCode.Created);
            }
        }

        public Task<ApiResponse<T>> UpdateAsync(int id, T entity, CancellationToken ct = default)
        {
            lock (_store.SyncRoot)
            {
                var index = Table.FindIndex(x => x.Id == id);
                if (index < 0) return NotFound<T>();

                var copy = Clone(entity);
                copy.Id = id;
                Table[index] = copy;
                return Ok(Clone(copy));
            }
        }

        public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default)
        {
            lock (_store.SyncRoot)
            {
                var removed = Table.RemoveAll(x => x.Id == id) > 0;
                return removed
                    ? Task.FromResult(new ApiResponse<object> { IsSuccess = true, HttpStatusCode = HttpStatusCode.NoContent })
                    : NotFound<object>();
            }
        }

        // Отдаём наружу копии, а не сами объекты из хранилища. Иначе страница,
        // изменив полученный объект, незаметно изменила бы и "базу".
        private static T Clone(T item) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item))!;

        // Task.FromResult — готовый Task с результатом: метод синхронный,
        // но должен соответствовать асинхронному интерфейсу.
        private static Task<ApiResponse<TResult>> Ok<TResult>(TResult result, HttpStatusCode code = HttpStatusCode.OK) =>
            Task.FromResult(new ApiResponse<TResult> { IsSuccess = true, HttpStatusCode = code, Result = result });

        private static Task<ApiResponse<TResult>> NotFound<TResult>() =>
            Task.FromResult(new ApiResponse<TResult> { IsSuccess = false, HttpStatusCode = HttpStatusCode.NotFound, ErrorMessage = "Запис не знайдено" });
    }
}
