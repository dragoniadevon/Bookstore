using Bookstore.Logger;

// Bookstore.Logger — отдельная программа для централизованного хранения логов.
//
// Это не веб-сайт и не API, а "Worker Service" — фоновая служба без интерфейса.
// Она запускается и просто работает в фоне (как служба Windows).
//
// План (см. TODO.md): Bookstore.Api будет отправлять сообщения в RabbitMQ,
// а Logger — забирать их оттуда и сохранять в отдельное хранилище.
// Пока это заготовка из шаблона проекта.
internal class Program
{
    private static void Main(string[] args)
    {
        // Host — "хозяин" приложения: даёт настройки, логирование и Dependency Injection,
        // как WebApplication в API, только без HTTP.
        var builder = Host.CreateApplicationBuilder(args);

        // Регистрируем фоновую задачу Worker. Host запустит её при старте
        // и остановит при завершении программы.
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();

        // Запускаем и работаем, пока программу не остановят (например, Ctrl+C).
        host.Run();
    }
}
