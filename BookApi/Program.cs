using Bookstore.Api;
using Bookstore.Api.Data;
using Bookstore.Api.Models;
using Bookstore.Api.Repositories;
using Bookstore.Api.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    public static void Main(string[] args)
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


        // Добавляем поддержку Controllers.
        //
        // Благодаря этому ASP.NET Core сможет находить наши
        // классы-контроллеры и обрабатывать HTTP-запросы.
        builder.Services.AddControllers();


        // Добавляем OpenAPI.
        //
        // Используется для описания API и документации
        // доступных HTTP endpoints.
        builder.Services.AddOpenApi();


        // ============================================================
        //                  BUILD APPLICATION
        // ============================================================

        // Создаём готовое приложение на основе всех настроек,
        // которые мы указали выше.
        var app = builder.Build();


        // ============================================================
        //              HTTP REQUEST PIPELINE
        // ============================================================

        // Проверяем, запущено ли приложение в режиме Development.
        //
        // В режиме разработки подключаем OpenAPI,
        // чтобы можно было просматривать описание API.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }


        // Перенаправляем HTTP-запросы на HTTPS.
        //
        // Например:
        // http://localhost:5000
        //          ↓
        // https://localhost:5001
        app.UseHttpsRedirection();


        // Включаем middleware авторизации.
        //
        // Здесь ASP.NET Core будет проверять,
        // имеет ли пользователь необходимые права доступа.
        //
        // В будущем здесь может использоваться вместе с
        // authentication / JWT.
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