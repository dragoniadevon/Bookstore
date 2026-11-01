using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
    {
        // Сущность User — пользователь (покупатель) магазина.
        //
        // У пользователя одна корзина (Cart) и много заказов (Orders).
        public class User : BaseEntity
        {
            // Имя пользователя
            public string Name { get; set; } = string.Empty;

            // Email — по нему пользователь входит в систему и получает письма
            public string Email { get; set; } = string.Empty;

            // Пароль.
            // ВАЖНО: настоящий пароль хранить в базе нельзя — если базу украдут,
            // украдут и пароли. Хранят только хеш (результат одностороннего
            // преобразования), например через PasswordHasher<User> из ASP.NET Core.
            public string Password { get; set; } = string.Empty;

            // Навигационное свойство: корзина пользователя.
            // Cart? — корзины может ещё не быть (пользователь ничего не добавлял).
            public Cart? Cart { get; set; }

            // Навигационное свойство: все заказы пользователя
            public IEnumerable<Order>? Orders { get; set; }

        }
    }
