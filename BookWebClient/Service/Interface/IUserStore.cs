using Bookstore.WebClient.Models;

namespace Bookstore.WebClient.Service.Interface
{
    // Хранилище пользователей.
    // Когда в API появится авторизация, достаточно написать реализацию,
    // которая ходит в API, и поменять регистрацию в Program.cs.
    public interface IUserStore
    {
        // Зарегистрировать пользователя. null — такой email уже занят.
        ShopUser? Register(string name, string email, string password);

        // Проверить email и пароль. null — неверные данные.
        ShopUser? ValidateCredentials(string email, string password);
    }
}
