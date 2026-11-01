using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Service.Interface;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Service
{
    // Базовый сервис: собирает ApiRequest и отправляет его через IApiGateWay.
    // Наследнику достаточно указать адрес контроллера, например "api/Author".
    //
    // abstract — от этого класса нельзя создать объект напрямую (new BaseApiService
    // не сработает). Он нужен только как основа для AuthorService, BookService и т.д.
    //
    // Зачем так? Запросы для всех сущностей одинаковые, отличается только адрес.
    // Код написан один раз здесь, а наследники его просто получают.
    // Если появится новая сущность, хватит класса из трёх строк.
    public abstract class BaseApiService<T> : IBaseApiService<T> where T : class
    {
        // Шлюз, который реально отправляет HTTP-запросы
        private readonly IApiGateWay _gateway;

        // Адрес контроллера API, например "api/Book"
        private readonly string _endpoint;

        // protected — конструктор может вызвать только наследник
        // (через ": base(gateway, "api/Book")").
        protected BaseApiService(IApiGateWay gateway, string endpoint)
        {
            _gateway = gateway;
            _endpoint = endpoint;
        }

        // GET api/Book -> список всех записей.
        //
        // Метод не помечен async и не использует await: он просто возвращает
        // Task от шлюза. Ждать его будет тот, кто вызвал сервис.
        public Task<ApiResponse<List<T>>> GetAllAsync(CancellationToken ct = default)
        {
            return _gateway.ApiRequest<List<T>>(new ApiRequest
            {
                ApiType = ApiType.GET,
                URL = _endpoint
            }, ct);
        }

        // GET api/Book/5 -> одна запись.
        // $"..." — интерполяция строк: {id} подставляет значение переменной.
        public Task<ApiResponse<T>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _gateway.ApiRequest<T>(new ApiRequest
            {
                ApiType = ApiType.GET,
                URL = $"{_endpoint}/{id}"
            }, ct);
        }

        // POST api/Book + объект в теле -> создание записи
        public Task<ApiResponse<T>> CreateAsync(T entity, CancellationToken ct = default)
        {
            return _gateway.ApiRequest<T>(new ApiRequest
            {
                ApiType = ApiType.POST,
                URL = _endpoint,
                Data = entity
            }, ct);
        }

        // PUT api/Book/5 + объект в теле -> обновление записи
        public Task<ApiResponse<T>> UpdateAsync(int id, T entity, CancellationToken ct = default)
        {
            return _gateway.ApiRequest<T>(new ApiRequest
            {
                ApiType = ApiType.PUT,
                URL = $"{_endpoint}/{id}",
                Data = entity
            }, ct);
        }

        // DELETE api/Book/5 -> удаление записи
        public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default)
        {
            return _gateway.ApiRequest<object>(new ApiRequest
            {
                ApiType = ApiType.DELETE,
                URL = $"{_endpoint}/{id}"
            }, ct);
        }
    }
}
