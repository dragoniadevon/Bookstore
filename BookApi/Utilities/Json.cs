using Newtonsoft.Json;

namespace Bookstore.Api.Utilities
{
    // Вспомогательный класс для превращения любого объекта в JSON-строку.
    //
    // static class — у класса нельзя создать объект через new,
    // в нём только статические методы, которые вызываются напрямую.
    public static class Json // нужен чтобы превращать объекты в json, но пока не знаю куда лучше применить
    {
        // Метод расширения (extension method).
        // Слово "this" перед параметром позволяет вызывать метод так,
        // будто он есть у самого объекта:
        //
        //     var json = author.ToJson();
        //
        // вместо Json.ToJson(author).
        //
        // Пригодится, например, чтобы записывать объекты в лог
        // или отправлять сообщения в RabbitMQ.
        // Контроллерам он не нужен: ASP.NET Core сам превращает
        // возвращаемые объекты в JSON.
        public static string ToJson(this object obj) {

            return JsonConvert.SerializeObject(obj);
        }
    }
}
