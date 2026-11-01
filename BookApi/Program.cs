using Bookstore.Api;
using Bookstore.Api.Data;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Bookstore.Api.Services;
using Bookstore.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    // async Task — Main стал асинхронным, чтобы можно было дождаться (await)
    // заполнения базы демо-данными перед запуском приложения.
    public static async Task Main(string[] args)
    {
        // Создаём builder приложения.
        //
        // Через builder мы регистрируем все необходимые сервисы
        // и настраиваем наше ASP.NET Core приложение.
        var builder = WebApplication.CreateBuilder(args);
         

        // ============================================================
        //                  REGISTRATION OF SERVICES
        // ============================================================


        // Регистрируем AppDbContext в Dependency Injection.
        //
        // Теперь ASP.NET Core сможет автоматически создавать
        // AppDbContext там, где он понадобится.
        //
        // Например:
        //
        // public AuthorRepository(AppDbContext dbContext)
        //
        // ASP.NET Core сам передаст туда зарегистрированный
        // экземпляр AppDbContext.
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            // Указываем, что для работы с базой данных
            // используется SQL Server.
            //
            // Строка подключения берётся из appsettings.json
            // по ключу "DefaultConnection".
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")
            );
        });


        // Регистрируем AutoMapper в Dependency Injection.
        //
        // MappingConfig содержит правила преобразования
        // одних объектов в другие.
        //
        // Например:
        // Author -> AuthorDto
        // AuthorDto -> Author
        builder.Services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingConfig>();
        });


        // Регистрируем репозиторий в Dependency Injection.
        //
        // Когда где-нибудь в приложении понадобится:
        //
        // IBaseRepository<Author>
        //
        // ASP.NET Core создаст и передаст:
        //
        // AuthorRepository
        //
        // Scoped означает, что в рамках одного HTTP-запроса
        // будет использоваться один экземпляр этого сервиса.
        builder.Services.AddScoped<IBaseRepository<Author>, AuthorRepository>();

        // Сервис регистрируется под интерфейсом с двумя типами:
        // обычный DTO и DTO для PATCH (в нём все поля необязательные).
        builder.Services.AddScoped<IBaseService<AuthorDTO, AuthorPatchDTO>, AuthorService>();

        // Book: репозиторий и сервис.
        builder.Services.AddScoped<IBaseRepository<Book>, BookRepository>();
        builder.Services.AddScoped<IBaseService<BookDTO, BookPatchDTO>, BookService>();

        // Genre: репозиторий и сервис.
        // GenreRepository нужен и самому GenreService, и BookService
        // (тот проверяет, что жанр существует).
        builder.Services.AddScoped<IBaseRepository<Genre>, GenreRepository>();
        builder.Services.AddScoped<IBaseService<GenreDTO, GenrePatchDTO>, GenreService>();


        // Publisher: репозиторий и сервис.
        builder.Services.AddScoped<IBaseRepository<Publisher>, PublisherRepository>();
        builder.Services.AddScoped<IBaseService<PublisherDTO, PublisherPatchDTO>, PublisherService>();

        // Пользователи, корзина и заказы.
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddScoped<IOrderService, OrderService>();


        // ------------------------------------------------------------
        //   Аутентификация через JWT
        // ------------------------------------------------------------
        //
        // JwtSettings (ключ, срок действия) читаем из настройки "Jwt".
        // Регистрируем через фабрику (sp => ...), а не читаем сразу: так настройки
        // берутся в момент первого использования, и тесты успевают подставить свои.
        builder.Services.AddSingleton(sp => JwtSettings.FromConfiguration(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHostEnvironment>()));
        builder.Services.AddSingleton<ITokenService, TokenService>();

        // Схема "Bearer": токен приходит в заголовке Authorization: Bearer <токен>.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Правила проверки токена. Configure<JwtSettings> — получить JwtSettings из DI.
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, jwt) =>
            {
                // Не переименовывать claims в длинные URL (sub -> ClaimTypes.NameIdentifier):
                // используем короткие имена из AppClaims.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.SigningKey,   // подпись проверяется нашим ключом
                    ValidateLifetime = true,             // просроченный токен не принимается
                    ClockSkew = TimeSpan.FromMinutes(1), // допуск на разницу часов

                    // Из какого claim брать имя (User.Identity.Name) и роль (User.IsInRole)
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Role
                };
            });

        builder.Services.AddAuthorization();

        // Добавляем поддержку Controllers.
        //
        // Благодаря этому ASP.NET Core сможет находить наши
        // классы-контроллеры и обрабатывать HTTP-запросы.
        builder.Services.AddControllers();


        // Добавляем OpenAPI.
        //
        // Используется для описания API и документации
        // доступных HTTP endpoints.
        //builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();

        // Swagger с кнопкой "Authorize": в неё вставляется токен из /api/Auth/login,
        // и Swagger добавляет его ко всем запросам — так можно проверять методы с [Authorize].
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Вставьте токен из ответа /api/Auth/login (без слова Bearer)."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        // ============================================================
        //                  BUILD APPLICATION
        // ============================================================

        // Создаём готовое приложение на основе всех настроек,
        // которые мы указали выше.
        var app = builder.Build();


        // В режиме разработки заполняем пустую базу демонстрационным каталогом
        // (украинские и зарубежные книги). Если книги уже есть — ничего не происходит.
        //
        // CreateScope — AppDbContext зарегистрирован как Scoped ("на один запрос"),
        // а при запуске запроса ещё нет, поэтому создаём область вручную.
        // using — область (и DbContext в ней) освободится после заполнения.
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbSeeder.SeedDemoCatalogAsync(db);
            await DbSeeder.SeedDemoUsersAsync(db);
        }

        // Предупреждаем, если ключ JWT не задан и создан временный
        if (app.Services.GetRequiredService<JwtSettings>().IsTemporaryKey)
        {
            app.Logger.LogWarning(
                "Ключ Jwt:Key не задан — используется временный ключ, токены перестанут действовать после перезапуска. " +
                "Задайте ключ: dotnet user-secrets set \"Jwt:Key\" \"<строка не короче 32 символов>\" --project BookApi");
        }


        // ============================================================
        //              HTTP REQUEST PIPELINE
        // ============================================================

        // Проверяем, запущено ли приложение в режиме Development.
        //
        // В режиме разработки подключаем OpenAPI,
        // чтобы можно было просматривать описание API.
        if (app.Environment.IsDevelopment())
        {
            //app.MapOpenApi();

            app.UseSwagger();
            app.UseSwaggerUI();
        }


        // Перенаправляем HTTP-запросы на HTTPS (только на рабочем сервере).
        //
        // Например:
        // http://localhost:5000
        //          ↓
        // https://localhost:7000
        //
        // При разработке клиент обращается к http://localhost:5000. При перенаправлении
        // HttpClient из соображений безопасности НЕ переносит заголовок Authorization
        // на новый адрес — токен потерялся бы, и все запросы с [Authorize] получали бы 401.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }


        // Аутентификация: читает токен из заголовка Authorization, проверяет подпись
        // и срок и заполняет User (кто делает запрос).
        // Должна стоять ДО UseAuthorization: сначала узнаём, кто это, потом проверяем права.
        app.UseAuthentication();

        // Авторизация: проверяет атрибуты [Authorize] и [Authorize(Roles = ...)].
        // Нет токена — 401 Unauthorized, нет нужной роли — 403 Forbidden.
        app.UseAuthorization();


        // Подключаем наши Controllers к HTTP pipeline.
        //
        // Благодаря этому запросы вроде:
        //
        // GET /api/authors
        //
        // смогут попасть в соответствующий Controller.
        app.MapControllers();


        // Запускаем веб-приложение.
        //
        // После этого приложение начинает принимать
        // HTTP-запросы.
        app.Run();
    }
}