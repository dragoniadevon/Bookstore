using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Infrastructure.Infrastracture;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Service;
using Bookstore.WebClient.Service.Interface;
using Bookstore.WebClient.Service.Mock;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

// Program.cs — точка входа веб-клиента. С этого файла начинается запуск приложения.
//
// Здесь две части:
// 1. Регистрация сервисов (до builder.Build()) — говорим приложению,
//    какие классы ему понадобятся и как их создавать.
// 2. Настройка конвейера запросов (после builder.Build()) — говорим,
//    через какие шаги проходит каждый HTTP-запрос от браузера.
//
// В отличие от Bookstore.Api, здесь нет базы данных: все данные
// WebClient получает, отправляя HTTP-запросы в API.

// builder — "строитель" приложения: через него регистрируют сервисы
// и читают настройки из appsettings.json.
var builder = WebApplication.CreateBuilder(args);

// ============================================================
//                  REGISTRATION OF SERVICES
// ============================================================

// Подключаем Razor Pages — страницы из папки Pages (.cshtml + .cshtml.cs).
//
// Conventions — правила доступа к страницам сразу для целых папок:
// - оформление заказа и "Мои заказы" — только для вошедших пользователей;
// - папка Admin — только для администраторов (политика "AdminOnly" ниже).
// Если гость откроет такую страницу, его перекинет на /Account/Login.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Checkout");
    options.Conventions.AuthorizePage("/Account/Orders");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
})
.AddMvcOptions(options =>
{
    // Стандартные сообщения ASP.NET Core о неверных значениях в формах — по-английски
    // ("The value '' is invalid."). Заменяем их на украинские. {0} — введённое значение
    // или название поля (из [Display(Name = ...)]).
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustNotBeNullAccessor(_ => "Поле обов'язкове.");
    messages.SetMissingBindRequiredValueAccessor(field => $"Не вказано значення для «{field}».");
    messages.SetMissingKeyOrValueAccessor(() => "Поле обов'язкове.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значення «{value}» не підходить для поля «{field}».");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Неправильне значення поля «{field}».");
    messages.SetValueIsInvalidAccessor(value => $"Значення «{value}» неправильне.");
    messages.SetValueMustBeANumberAccessor(field => $"Поле «{field}» має бути числом.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Значення «{value}» неправильне.");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Неправильне значення.");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "Значення має бути числом.");
});

// Аутентификация через cookie: после входа браузер получает зашифрованную cookie,
// по которой сервер узнаёт пользователя в следующих запросах.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Авторизация: политика "AdminOnly" пускает только пользователей с ролью Admin.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(ShopRoles.Admin));
});

// Сессия — для корзины. Данные сессии хранятся в памяти сервера,
// а браузер получает только cookie с её номером.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;   // cookie недоступна из JavaScript
    options.Cookie.IsEssential = true; // нужна для работы сайта
});

// Даёт сервисам (например, корзине) доступ к текущему HTTP-запросу.
builder.Services.AddHttpContextAccessor();

// По умолчанию Razor выводит кириллицу как "&#x43A;&#x43D;..." — браузер покажет
// её правильно, но исходный код страницы не прочитать. Разрешаем все символы Unicode.
builder.Services.AddWebEncoders(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));


// Адрес API берём из настроек (appsettings.json), а не пишем прямо в коде:
// на компьютере разработчика и на сервере адреса разные.
//
// Если ключа нет или в нём не адрес (пусто, нет "https://"), берём адрес API
// из его launchSettings.json. Так сайт запускается и в мок-режиме,
// когда настройка API вообще не нужна.
//
// Uri.TryCreate не бросает исключение, а возвращает false, если строка — не адрес.
// UriKind.Absolute — нужен полный адрес со схемой и хостом.
const string defaultApiUrl = "http://localhost:5000/";
var configuredApiUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUri = Uri.TryCreate(configuredApiUrl, UriKind.Absolute, out var parsedApiUri)
    ? parsedApiUri
    : new Uri(defaultApiUrl);

// Регистрируем HttpClient для обращений к API.
//
// Почему не "new HttpClient()" в каждом месте? Если создавать и удалять
// HttpClient на каждый запрос, у сервера могут закончиться сетевые соединения.
// IHttpClientFactory сам правильно переиспользует соединения.
//
// ApiClientName — имя клиента (константа из AppConstants), по нему
// ApiGateway потом получает этот клиент. BaseAddress — общая часть адреса,
// к которой дописываются пути вроде "api/Author".
builder.Services.AddHttpClient(ApiClientName, client => { client.BaseAddress = apiBaseUri; });

// Dependency Injection (внедрение зависимостей).
//
// Запись AddScoped<Интерфейс, Класс>() означает:
// "когда кто-то попросит Интерфейс в конструкторе — создай и передай Класс".
//
// Scoped — один объект на один HTTP-запрос. Внутри запроса все получают
// один и тот же экземпляр, для следующего запроса создаётся новый.

// Шлюз к API: единое место, через которое уходят все HTTP-запросы.
builder.Services.AddScoped<IApiGateWay, ApiGateway>();

// ------------------------------------------------------------
//   Источник данных: мок или API
// ------------------------------------------------------------
//
// Страница просит, например, IBaseApiService<BookDto> и получает SwitchableApiService,
// который на каждый вызов смотрит на переключатель и отправляет запрос:
// - в режиме Api  -> BookService -> ApiGateway -> Bookstore.Api;
// - в режиме Mock -> MockApiService -> MockDataStore (данные в памяти).
//
// Режим по умолчанию — из настройки "DataSource:Mode" (Mock / Api), иначе Api.
// Посетитель может переключить его кнопкой в шапке сайта.
builder.Services.AddScoped<IDataSourceSwitch, SessionDataSourceSwitch>();
builder.Services.AddSingleton<MockDataStore>();
builder.Services.AddScoped(typeof(MockApiService<>));

AddSwitchable<AuthorDto, AuthorService>(builder.Services);
AddSwitchable<BookDto, BookService>(builder.Services);
AddSwitchable<GenreDto, GenreService>(builder.Services);
AddSwitchable<PublisherDto, PublisherService>(builder.Services);

// Эмуляция оплаты (реальных платежей нет).
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Витрина магазина: книги с именами авторов, жанров и издательств.
builder.Services.AddScoped<ICatalogService, CatalogService>();

// ------------------------------------------------------------
//   Корзина, вход и заказы: API или мок
// ------------------------------------------------------------
//
// Для каждой функции две реализации и "стрелочник" (Switchable...), который
// выбирает нужную по режиму из переключателя "Мок / API":
// - режим API  -> ApiCartService / ApiAuthService / ApiOrderService (HTTP к Bookstore.Api);
// - режим Mock -> SessionCartService / MockAuthService / MockOrderService (данные в памяти).
// Гость в режиме API пользуется корзиной в сессии; после входа она переносится в API (CartMerger).

// Корзина
builder.Services.AddScoped<SessionCartService>();
builder.Services.AddScoped<ApiCartService>();
builder.Services.AddScoped<ICartService, SwitchableCartService>();
builder.Services.AddScoped<CartMerger>();

// Вход и регистрация
builder.Services.AddScoped<ApiAuthService>();
builder.Services.AddScoped<MockAuthService>();
builder.Services.AddScoped<IAuthService, SwitchableAuthService>();

// Заказы
builder.Services.AddScoped<ApiOrderService>();
builder.Services.AddScoped<MockOrderService>();
builder.Services.AddScoped<IOrderService, SwitchableOrderService>();

// Хранилища мок-режима: пользователи и заказы в памяти (пропадают при перезапуске).
// Singleton — один объект на всё приложение, чтобы данные не терялись между запросами.
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();






// Собираем приложение из всего, что зарегистрировали выше.
// После этой строки добавлять сервисы уже нельзя.
var app = builder.Build();

// Предупреждаем в логе, если адрес API в настройках задан неправильно
if (!string.Equals(configuredApiUrl, apiBaseUri.OriginalString, StringComparison.Ordinal))
{
    app.Logger.LogWarning(
        "Настройка Api:BaseUrl не задана или не является адресом. Используется {ApiUrl}. Пример значения: \"http://localhost:5000/\"",
        apiBaseUri);
}

// ============================================================
//                  HTTP REQUEST PIPELINE
// ============================================================
//
// Конвейер (pipeline) — цепочка шагов, через которую проходит каждый запрос.
// Порядок важен: шаги выполняются сверху вниз.

// В режиме разработки (Development) ASP.NET Core показывает подробную
// страницу ошибки — удобно для отладки. На "боевом" сервере её показывать
// нельзя (там видно код и детали), поэтому вместо неё показываем /Error.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // HSTS — говорит браузеру: "к этому сайту обращайся только по HTTPS".
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


// Числа в формах разбираем в "инвариантном" формате (с точкой: 690.50).
// Иначе на компьютере с русской локалью ASP.NET Core ждал бы запятую,
// а поля <input type="number"> всегда отправляют точку — цена не сохранилась бы.
// Цены на страницах всё равно выводятся в гривнах в украинском формате (см. Formatting.ToUah).
var invariant = CultureInfo.InvariantCulture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(invariant),
    SupportedCultures = [invariant],
    SupportedUICultures = [invariant]
});

// Если пользователь зашёл по http://, перенаправляем на https://.
app.UseHttpsRedirection();

// Определяет, какая страница должна обработать запрос (по адресу).
app.UseRouting();

// Включает сессию (корзина). Должна стоять до страниц, которые ею пользуются.
app.UseSession();

// Аутентификация: читает cookie и определяет, кто пользователь.
// Должна стоять ДО UseAuthorization — сначала узнаём, кто это, потом проверяем права.
app.UseAuthentication();

// Проверяет права доступа (правила из AddRazorPages выше).
app.UseAuthorization();

// Отдаёт статические файлы из wwwroot (css, js, картинки, bootstrap).
app.MapStaticAssets();

// Подключает Razor Pages к адресам: Pages/Index.cshtml -> "/", Pages/Privacy.cshtml -> "/Privacy".
app.MapRazorPages().WithStaticAssets();

// Запускаем приложение: оно начинает принимать запросы и работает,
// пока его не остановят.
app.Run();

// Регистрирует API-сервис сущности и "стрелочник" перед ним.
// Локальная функция: объявлена прямо в Program.cs, видна только здесь.
static void AddSwitchable<TDto, TApiService>(IServiceCollection services)
    where TDto : class, IEntityDto
    where TApiService : BaseApiService<TDto>
{
    // Сам API-сервис (например, BookService) — под своим классом и под BaseApiService<TDto>
    services.AddScoped<TApiService>();
    services.AddScoped<BaseApiService<TDto>>(sp => sp.GetRequiredService<TApiService>());

    // То, что получают страницы, — переключаемый сервис
    services.AddScoped<IBaseApiService<TDto>, SwitchableApiService<TDto>>();
}
