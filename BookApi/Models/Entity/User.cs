using Bookstore.Api.Models.Entity.Base;

namespace Bookstore.Api.Models.Entity
{
    // Сущность User — пользователь (покупатель или администратор) магазина.
    //
    // У пользователя одна корзина (Cart) и много заказов (Orders).
    public class User : BaseEntity
    {
        // Имя пользователя
        public string Name { get; set; } = string.Empty;

        // Email — по нему пользователь входит в систему. Уникален (индекс в AppDbContext).
        public string Email { get; set; } = string.Empty;

        // Хеш пароля, а НЕ сам пароль.
        //
        // Хеш — результат одностороннего преобразования: по нему нельзя узнать пароль,
        // но можно проверить, что пользователь ввёл правильный. Если базу украдут,
        // пароли останутся в безопасности. Хеш создаёт PasswordHasher<User> (см. AuthService).
        public string PasswordHash { get; set; } = string.Empty;

        // Роль: "Customer" (покупатель) или "Admin" (может менять каталог).
        // Названия ролей — в UserRoles, чтобы не опечататься в строках.
        public string Role { get; set; } = UserRoles.Customer;

        // Навигационное свойство: корзина пользователя.
        // Cart? — корзины может ещё не быть (пользователь ничего не добавлял).
        public Cart? Cart { get; set; }

        // Навигационное свойство: все заказы пользователя
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
