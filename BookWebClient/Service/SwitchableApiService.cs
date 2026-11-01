using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service.Interface;
using Bookstore.WebClient.Service.Mock;

namespace Bookstore.WebClient.Service
{
    // "Стрелочник": на каждый вызов смотрит на переключатель и передаёт
    // запрос либо настоящему API-сервису, либо мок-сервису.
    //
    // Страницы получают IBaseApiService<BookDto> и не знают, откуда данные.
    // Это паттерн "Прокси" (Proxy): объект с тем же интерфейсом,
    // который решает, кому передать работу.
    public class SwitchableApiService<T> : IBaseApiService<T> where T : class, IEntityDto
    {
        private readonly IDataSourceSwitch _switch;
        // Тип именно BaseApiService<T>, а не IBaseApiService<T>: под интерфейсом
        // в DI зарегистрирован сам SwitchableApiService, и он получил бы сам себя.
        private readonly BaseApiService<T> _api;
        private readonly MockApiService<T> _mock;

        public SwitchableApiService(IDataSourceSwitch dataSourceSwitch, BaseApiService<T> api, MockApiService<T> mock)
        {
            _switch = dataSourceSwitch;
            _api = api;
            _mock = mock;
        }

        // Текущий источник выбирается при каждом вызове
        private IBaseApiService<T> Current => _switch.Mode == DataSourceMode.Mock ? _mock : _api;

        public Task<ApiResponse<List<T>>> GetAllAsync(CancellationToken ct = default) => Current.GetAllAsync(ct);
        public Task<ApiResponse<T>> GetByIdAsync(int id, CancellationToken ct = default) => Current.GetByIdAsync(id, ct);
        public Task<ApiResponse<T>> CreateAsync(T entity, CancellationToken ct = default) => Current.CreateAsync(entity, ct);
        public Task<ApiResponse<T>> UpdateAsync(int id, T entity, CancellationToken ct = default) => Current.UpdateAsync(id, entity, ct);
        public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default) => Current.DeleteAsync(id, ct);
    }
}
