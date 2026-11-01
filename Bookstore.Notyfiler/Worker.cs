namespace Bookstore.Notyfiler
{
    // Worker — фоновая задача, которая работает всё время, пока запущена программа.
    // Подробное описание BackgroundService — в Bookstore.Logger/Worker.cs.
    //
    // Логгер передаётся через первичный конструктор и Dependency Injection.
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        // Вызывается один раз при запуске. stoppingToken — сигнал остановки.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Заглушка из шаблона: раз в секунду пишет в лог текущее время.
            // Позже здесь будет получение событий из RabbitMQ и отправка писем.
            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                // Пауза 1 секунда; прерывается сразу, если программу останавливают.
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
