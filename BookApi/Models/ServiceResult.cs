namespace Bookstore.Api.Models
{
    // Чем закончилась операция сервиса. Controller превращает это в HTTP-код.
    public enum ServiceStatus
    {
        Ok,           // 200
        NotFound,     // 404 — книга/заказ не найдены
        BadRequest,   // 400 — например, оформление пустой корзины
        Conflict,     // 409 — например, не хватает книг на складе, email уже занят
        Unauthorized  // 401 — неверный email или пароль
    }

    // Результат операции: статус + данные (если успех) или текст ошибки.
    //
    // Зачем, если есть null и DeleteResult? Для корзины и заказов причин
    // неудачи много (нет книги, мало на складе, корзина пуста...), и клиенту
    // нужно сообщить, какая именно. Service при этом всё так же не знает про HTTP.
    public class ServiceResult<T>
    {
        public ServiceStatus Status { get; private init; }
        public T? Value { get; private init; }
        public string? Error { get; private init; }

        public bool IsSuccess => Status == ServiceStatus.Ok;

        // Фабричные методы: ServiceResult<CartDTO>.Ok(cart), ServiceResult<CartDTO>.Fail(...)
        public static ServiceResult<T> Ok(T value) => new() { Status = ServiceStatus.Ok, Value = value };

        public static ServiceResult<T> Fail(ServiceStatus status, string error) => new() { Status = status, Error = error };
    }
}
