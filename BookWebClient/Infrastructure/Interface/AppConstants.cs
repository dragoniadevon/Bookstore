namespace Bookstore.WebClient.Infrastructure.Interface
{
    // Общие константы приложения.
    //
    // Значения, которые используются в нескольких местах, держим в одном месте:
    // если строку "BookStoreApi" написать вручную в двух файлах и в одном ошибиться,
    // программа соберётся, но работать не будет. С константой такого не случится.
    public static class AppConstants
    {
        // enum (перечисление) — набор именованных вариантов.
        // Вместо строк "GET"/"POST", в которых легко опечататься,
        // пишем ApiType.GET — компилятор проверит, что такой вариант есть.
        public enum ApiType
        {
            GET, POST, PUT, DELETE
        }

        // Ключ, под которым JWT-токен будет храниться в сессии пользователя.
        public static string SeesionToken = "JWTToken";

        // Имя HttpClient для обращений к API.
        // const — значение задаётся один раз при компиляции и не может измениться.
        public const string ApiClientName = "BookStoreApi";
    }
}
