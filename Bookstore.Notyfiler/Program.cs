using Bookstore.Notyfiler;

// Bookstore.Notyfiler — отдельная фоновая служба для отправки уведомлений
// (например, писем на почту: "Ваш заказ оплачен").
//
// План (см. TODO.md): Bookstore.Api публикует событие в RabbitMQ,
// Notyfiler его получает и отправляет письмо. Благодаря этому API не ждёт,
// пока письмо уйдёт, и пользователь быстрее получает ответ.
// Пока это заготовка из шаблона проекта.
//
// Здесь используются top-level statements: код пишется сразу, без класса Program
// и метода Main — компилятор создаст их сам. В Bookstore.Logger то же самое
// записано "по-старому", с явным Main.

// Host даёт настройки, логирование и Dependency Injection (как в API, но без HTTP).
var builder = Host.CreateApplicationBuilder(args);

// Регистрируем фоновую задачу Worker.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Запускаем и работаем, пока программу не остановят.
host.Run();
