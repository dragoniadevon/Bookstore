namespace Bookstore.WebClient.Models
{
    // Пользователь магазина (демо-версия, хранится в памяти).
    public class ShopUser
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Храним не сам пароль, а его хеш — результат одностороннего преобразования.
        // По хешу нельзя узнать пароль, но можно проверить, что введён правильный.
        public string PasswordHash { get; set; } = string.Empty;

        // Роль: "Customer" — покупатель, "Admin" — может управлять книгами
        public string Role { get; set; } = ShopRoles.Customer;
    }

    // Названия ролей держим в одном месте, чтобы не опечататься в строках.
    public static class ShopRoles
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
    }
}
