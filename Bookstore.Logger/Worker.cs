namespace Bookstore.Logger
{
    // Worker — фоновая задача, которая работает всё время, пока запущена программа.
    //
    // BackgroundService — базовый класс .NET для таких задач.
    // Нужно переопределить один метод — ExecuteAsync, в нём и пишется вся работа.
    //
    // "Worker(ILogger<Worker> logger)" — первичный конструктор (primary constructor):
    // короткая запись, при которой параметр logger сразу доступен во всём классе.
    // Логгер передаёт Dependency Injection.
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        // Вызывается один раз при запуске программы.
        // stoppingToken — сигнал "программу останавливают, пора заканчивать".
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Крутимся в цикле, пока не попросят остановиться.
            // Сейчас это заглушка из шаблона: раз в секунду пишет в лог текущее время.
            // Позже здесь будет чтение сообщений из RabbitMQ и их сохранение.
            while (!stoppingToken.IsCancellationRequested)
            {
                // Проверяем, включён ли уровень Information, чтобы не тратить
                // ресурсы на подготовку сообщения, которое всё равно не запишется.
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                // Пауза 1 секунда. Передаём stoppingToken, чтобы пауза сразу
                // прервалась, если программу останавливают.
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
