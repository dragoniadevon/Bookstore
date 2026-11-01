namespace Bookstore.Api.Models
{
    // Названия ролей пользователей.
    //
    // const — значения известны при компиляции, поэтому их можно использовать
    // в атрибутах: [Authorize(Roles = UserRoles.Admin)].
    public static class UserRoles
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
    }
}
