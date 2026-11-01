# Bookstore — книжный интернет-магазин

Учебный проект на **ASP.NET Core (.NET 10)**: книжный интернет-магазин из двух основных частей.

- **Bookstore.Api** — серверная часть (REST API). Хранит каталог в базе данных SQL Server и отдаёт его по HTTP в формате JSON.
- **Bookstore.WebClient** — сайт магазина (Razor Pages). Показывает каталог, корзину, оформление заказа с оплатой, вход и регистрацию, админку для книг. С базой напрямую **не работает** — все данные получает из API по HTTP.

Интерфейс магазина — на украинском языке, цены — в гривнах (₴).

Дополнительно в решении есть заготовки **Bookstore.Logger** и **Bookstore.Notyfiler** (фоновые службы для логов и уведомлений, в будущем — через RabbitMQ, см. `TODO.md`).

---

## Содержание

1. [Как всё устроено (общая схема)](#как-всё-устроено-общая-схема)
2. [Структура решения](#структура-решения)
3. [Быстрый запуск](#быстрый-запуск)
4. [Блок API: как устроен и как повторить по шагам](#блок-api-как-устроен-и-как-повторить-по-шагам)
5. [Блок Клиент: как устроен и как повторить по шагам](#блок-клиент-как-устроен-и-как-повторить-по-шагам)
6. [Сквозные примеры: путь запроса от браузера до базы](#сквозные-примеры-путь-запроса-от-браузера-до-базы)
7. [Демо-данные для проверки](#демо-данные-для-проверки)
8. [Что ещё не сделано](#что-ещё-не-сделано)

---

## Как всё устроено (общая схема)

```
 Браузер
    │  HTTP (страницы, формы)
    ▼
┌──────────────────────────── Bookstore.WebClient (порт 5001) ────────────────────────────┐
│  Razor Page (.cshtml + PageModel)                                                       │
│        │                                                                                │
│        ▼                                                                                │
│  CatalogService / CartService / PaymentService ...                                      │
│        │                                                                                │
│        ▼                                                                                │
│  SwitchableApiService ──► режим "Mock" ──► MockApiService ──► MockDataStore (память)    │
│        │                                                                                │
│        └──────────────► режим "Api"  ──► BookService ──► ApiGateway ──► HttpClient ─────┼──┐
└─────────────────────────────────────────────────────────────────────────────────────────┘  │
                                                                         HTTP + JSON          │
┌──────────────────────────── Bookstore.Api (порт 5000) ──────────────────────────────────┐  │
│  Controller  ◄──────────────────────────────────────────────────────────────────────────┼──┘
│      │   (HTTP: принять запрос, выбрать код ответа)                                     │
│      ▼                                                                                  │
│  Service     (логика: проверки, DTO ↔ Entity через AutoMapper)                          │
│      │                                                                                  │
│      ▼                                                                                  │
│  Repository  (работа с базой через EF Core)                                             │
│      │                                                                                  │
│      ▼                                                                                  │
│  AppDbContext ──► SQL Server                                                            │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

Главная идея — **разделение ответственности по слоям**: каждый слой делает одну вещь и знает только о соседнем слое.

| Слой | За что отвечает | Чего НЕ делает |
|---|---|---|
| Controller | HTTP: принять запрос, вернуть код (200, 404, 409...) | Не ходит в базу |
| Service | Логика: проверки, преобразование DTO ↔ Entity | Не знает про HTTP-коды |
| Repository | Запросы к базе через EF Core | Не знает про DTO и HTTP |
| DbContext | Связь с SQL Server, таблицы, связи | — |

---

## Структура решения

```
Bookstore/
├── Dragonia.slnx                  — файл решения
├── dotnet-tools.json              — локальные инструменты (dotnet-ef для миграций)
├── TODO.md                        — планы по архитектуре (RabbitMQ, SignalR)
│
├── BookApi/                       — Bookstore.Api
│   ├── Controllers/               — AuthorController, BookController, GenreController, PublisherController
│   ├── Data/                      — AppDbContext (база), DbSeeder (демо-данные)
│   ├── Migrations/                — миграции EF Core (создаются командой, руками не правятся)
│   ├── Models/
│   │   ├── Entity/                — сущности = таблицы базы (Book, Author, Cart, Order...)
│   │   ├── DTO/                   — объекты для обмена с клиентом (BookDTO, BookPatchDTO...)
│   │   └── DeleteResult.cs        — результат удаления (Deleted / NotFound / InUse)
│   ├── Repositories/              — доступ к базе
│   ├── Services/                  — логика
│   ├── MappingConfig.cs           — правила AutoMapper (Entity ↔ DTO)
│   └── Program.cs                 — регистрация сервисов и запуск
│
├── BookWebClient/                 — Bookstore.WebClient
│   ├── Infrastructure/            — ApiGateway (HTTP к API), переключатель источника, форматирование цен
│   ├── Models/                    — DTO (зеркало API), корзина, заказ, пользователь, модели оплаты
│   ├── Service/                   — сервисы сущностей, каталог, корзина, оплата, пользователи, заказы
│   │   └── Mock/                  — мок-данные и мок-реализация API
│   ├── Pages/                     — Razor-страницы (каталог, книга, корзина, оформление, аккаунт, админка)
│   ├── wwwroot/                   — css, js, Bootstrap
│   └── Program.cs
│
├── Bookstore.Logger/              — заготовка фоновой службы логов
└── Bookstore.Notyfiler/           — заготовка фоновой службы уведомлений
```

---

## Быстрый запуск

**Нужно:** .NET 10 SDK, SQL Server (LocalDB, Express или Developer).

1. **Строка подключения к базе.** В `BookApi/appsettings.json` (или через user-secrets) задайте `ConnectionStrings:DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Bookstore;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```
   Пароли в репозиторий не коммитьте — для этого есть `dotnet user-secrets`.

2. **Восстановите инструменты и создайте базу:**
   ```bash
   dotnet tool restore
   dotnet ef database update --project BookApi
   ```

3. **Запустите API** (в отдельном терминале):
   ```bash
   cd BookApi
   dotnet run --launch-profile http
   ```
   API слушает `http://localhost:5000`, Swagger — `http://localhost:5000/swagger`.
   При первом запуске в режиме Development пустая база заполнится демо-каталогом (12 книг).

4. **Запустите клиент:**
   ```bash
   cd BookWebClient
   dotnet run --launch-profile http
   ```
   Откройте `http://localhost:5001`.

> Клиент можно запустить и **без API и базы**: нажмите в шапке сайта кнопку **«Мок»** — данные будут браться из памяти клиента.

---

## Блок API: как устроен и как повторить по шагам

### Шаг 1. Создать проект и подключить пакеты

```bash
dotnet new webapi -n Bookstore.Api -o BookApi --use-controllers
cd BookApi
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package AutoMapper
dotnet add package Swashbuckle.AspNetCore
```

- **EF Core + SqlServer** — ORM: работаем с базой через C#-классы, а не через SQL.
- **Tools** — команды миграций (`dotnet ef ...`).
- **AutoMapper** — автоматическое копирование полей между Entity и DTO.
- **Swashbuckle** — Swagger: страница для ручной проверки API в браузере.

### Шаг 2. Базовая сущность

Все таблицы имеют общие поля — выносим их в базовый класс `Models/Entity/Base/BaseEntity.cs`:

```csharp
public class BaseEntity
{
    public int Id { get; set; }               // первичный ключ, генерирует база
    public DateTime CreatedAt { get; set; }   // заполняется автоматически (шаг 4)
    public DateTime? ModifiedAt { get; set; }
}
```

**Зачем:** не повторять одни и те же поля в каждом классе и обрабатывать даты в одном месте.

### Шаг 3. Сущности (Entity) = таблицы базы

Каждый класс в `Models/Entity` — это таблица. Связи описываются парой свойств: **внешний ключ** (`AuthorId`) и **навигационное свойство** (`Author`).

```csharp
public class Book : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }        // для денег — только decimal
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }

    public int AuthorId { get; set; }         // внешний ключ (колонка в таблице Books)
    public Author Author { get; set; } = null!; // навигационное свойство (объект, колонки нет)

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;

    public int PublisherId { get; set; }
    public Publisher Publisher { get; set; } = null!;
}

public class Author : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Book> Books { get; set; } = new List<Book>(); // "один ко многим"
}
```

Все сущности проекта:

| Сущность | Что хранит | Связи |
|---|---|---|
| `Author`, `Genre`, `Publisher` | Справочники | У каждого много `Book` |
| `Book` | Книга | Один автор, жанр, издательство |
| `User` | Пользователь | Одна `Cart`, много `Order` |
| `Cart` → `CartItem` | Корзина и её позиции (книга + количество) | `CartItem` связывает корзину и книгу |
| `Order` → `OrderItem` | Заказ и позиции (с ценой на момент покупки) | `OrderItem` связывает заказ и книгу |

> Связь «многие ко многим» (корзина ↔ книги) всегда делается через промежуточную таблицу (`CartItem`), в которой можно хранить дополнительные данные (количество).

### Шаг 4. AppDbContext — связь с базой

`Data/AppDbContext.cs` описывает таблицы и правила:

```csharp
internal class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; }        // таблица Books
    public DbSet<Author> Authors { get; set; }
    // ... Genres, Publishers, Users, Carts, CartItems, Orders, OrderItems

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Деньги: decimal(18, 2)
        modelBuilder.Entity<Book>().Property(b => b.Price).HasPrecision(18, 2);

        // Нельзя удалить автора, пока у него есть книги
        modelBuilder.Entity<Book>()
            .HasOne(b => b.Author).WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Нельзя удалить книгу, которая есть в заказах (история покупок не должна пропасть)
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Book).WithMany()
            .HasForeignKey(i => i.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Начальные данные с фиксированным Id
        modelBuilder.Entity<Author>().HasData(new Author { Id = 1, Name = "Тарас Шевченко", CreatedAt = seedDate });
    }

    // Даты CreatedAt / ModifiedAt проставляются автоматически для ВСЕХ сущностей
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = now;
                entry.Property(e => e.CreatedAt).IsModified = false;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
```

Регистрация в `Program.cs` — строка подключения берётся из настроек по ключу `DefaultConnection`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Шаг 5. Миграции — создание и изменение таблиц

Миграция — это C#-файл с инструкциями «как изменить базу». Создаётся автоматически по разнице между моделью (шаги 3–4) и прошлой миграцией.

```bash
dotnet ef migrations add InitialCreate --project BookApi   # создать миграцию
dotnet ef database update --project BookApi                # применить к базе
dotnet ef migrations list --project BookApi                # посмотреть, какие применены (Pending = нет)
```

**Порядок всегда такой:** изменили Entity / `AppDbContext` → `migrations add` → **прочитали сгенерированный файл** (нет ли неожиданного удаления данных) → `database update`.

### Шаг 6. DTO — то, что API принимает и отдаёт

Entity наружу не отдаём: в ней служебные поля и связи. Для клиента делаем DTO с правилами проверки (`Models/DTO`):

```csharp
public class BookDTO                          // для GET, POST, PUT
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public int AuthorId { get; set; }
    // ... остальные поля
}

public class BookPatchDTO                     // для PATCH: все поля необязательные
{
    [MinLength(1), MaxLength(200)]
    public string? Title { get; set; }        // null = "не меняем"

    [Range(0, int.MaxValue)]
    public int? StockQuantity { get; set; }   // можно передать 0 — это не "пусто"
    // ...
}
```

**Зачем два DTO:** для PUT нужны все поля (`[Required]`), а для PATCH клиент присылает только то, что меняет. Если использовать для PATCH обычный DTO, запрос без `Title` получит 400.

### Шаг 7. AutoMapper — копирование Entity ↔ DTO

`MappingConfig.cs`:

```csharp
public class MappingConfig : Profile
{
    public MappingConfig()
    {
        CreateMap<Book, BookDTO>().ReverseMap();   // ReverseMap — в обе стороны
        CreateMap<Author, AuthorDTO>().ReverseMap();
        // ...
    }
}
```

```csharp
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());
```

### Шаг 8. Repository — запросы к базе

Общий интерфейс `Repositories/Interface/IBaseRepository.cs`:

```csharp
public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T obj);
    Task<T?> UpdateAsync(T obj);
    Task<DeleteResult> DeleteAsync(int id);   // Deleted / NotFound / InUse
}
```

Реализация на примере удаления автора (`Repositories/AuthorRepository.cs`):

```csharp
public async Task<DeleteResult> DeleteAsync(int id)
{
    var author = await _authors.FirstOrDefaultAsync(x => x.Id == id);
    if (author == null) return DeleteResult.NotFound;

    // Заранее проверяем ограничение Restrict, чтобы не получить 500 от базы
    if (await _dbContext.Books.AnyAsync(b => b.AuthorId == id)) return DeleteResult.InUse;

    _authors.Remove(author);
    await _dbContext.SaveChangesAsync();
    return DeleteResult.Deleted;
}
```

**Зачем `DeleteResult`, а не `bool`:** у «не удалось удалить» две причины, и клиенту нужно их различать (404 и 409).

### Шаг 9. Service — логика

Интерфейс `Services/Interface/IBaseService.cs` — два типа: обычный DTO и DTO для PATCH:

```csharp
public interface IBaseService<T, TPatch> where T : class where TPatch : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> CreateAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<T?> PatchAsync(int id, TPatch patch);
    Task<DeleteResult> DeleteAsync(int id);
}
```

Пример — создание книги (`Services/BookService.cs`):

```csharp
public async Task<BookDTO?> CreateAsync(BookDTO entity)
{
    // 1. Проверяем, что автор, жанр и издательство существуют
    if (!await AuthorGenrePublisherExistAsync(entity.AuthorId, entity.GenreId, entity.PublisherId))
        return null;                                   // Controller вернёт 400

    // 2. DTO -> Entity
    var book = _mapper.Map<Book>(entity);
    book.Id = 0;                                       // Id выдаёт база, а не клиент

    // 3. Сохраняем через Repository и возвращаем DTO
    var created = await _repository.CreateAsync(book);
    return _mapper.Map<BookDTO>(created);
}
```

Service сообщает о результате через `null` / `DeleteResult` — **про HTTP-коды он не знает**.

### Шаг 10. Controller — HTTP

`Controllers/BookController.cs`:

```csharp
[ApiController]                       // автоматическая проверка DTO ([Required] и т.д.) -> 400
[Route("api/[controller]")]           // BookController -> /api/Book
public class BookController : ControllerBase
{
    private readonly IBaseService<BookDTO, BookPatchDTO> _bookService;

    public BookController(IBaseService<BookDTO, BookPatchDTO> bookService)
        => _bookService = bookService;    // передаёт Dependency Injection

    [HttpGet]                         // GET /api/Book
    public async Task<IActionResult> GetAll() => Ok(await _bookService.GetAllAsync());

    [HttpPost]                        // POST /api/Book
    public async Task<IActionResult> CreateAsync([FromBody] BookDTO newBook)
    {
        var book = await _bookService.CreateAsync(newBook);
        if (book == null) return BadRequest("Автора, жанр або видавництво з указаним ID не знайдено.");
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);   // 201 + объект + Location
    }

    [HttpDelete("{id}")]              // DELETE /api/Book/5
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _bookService.DeleteAsync(id);
        return result switch
        {
            DeleteResult.Deleted => NoContent(),                       // 204
            DeleteResult.InUse => Conflict("Неможливо видалити книгу: вона є в оформлених замовленнях."), // 409
            _ => NotFound()                                            // 404
        };
    }
    // GetById, PutAsync, PatchAsync — по тому же принципу
}
```

Все эндпоинты API (одинаково для `Author`, `Book`, `Genre`, `Publisher`):

| Метод | Адрес | Ответ |
|---|---|---|
| GET | `/api/Book` | 200 + список |
| GET | `/api/Book/{id}` | 200 / 404 |
| POST | `/api/Book` | 201 + объект / 400 |
| PUT | `/api/Book/{id}` | 200 / 404 |
| PATCH | `/api/Book/{id}` | 200 / 404 |
| DELETE | `/api/Book/{id}` | 204 / 404 / 409 |

### Шаг 11. Program.cs — собрать всё вместе

```csharp
// 1. Регистрация сервисов (Dependency Injection)
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());

// "Когда кто-то попросит интерфейс — создай этот класс". Scoped = один объект на HTTP-запрос.
builder.Services.AddScoped<IBaseRepository<Book>, BookRepository>();
builder.Services.AddScoped<IBaseService<BookDTO, BookPatchDTO>, BookService>();
// ... то же для Author, Genre, Publisher

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Демо-данные в режиме разработки (только если в базе нет книг)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedDemoCatalogAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3. Конвейер запросов
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Шаг 12. Демо-данные (DbSeeder)

`Data/DbSeeder.cs` при запуске в Development добавляет каталог (12 книг украинских и зарубежных авторов), если книг в базе ещё нет. Авторы, жанры и издательства ищутся по имени и создаются, только если их нет — повторный запуск ничего не дублирует.

**Почему не только `HasData`:** `HasData` требует фиксированных Id и превращает каждое изменение каталога в новую миграцию. Для демо-каталога удобнее заполнять базу кодом.

### Шаг 13. Проверка

Откройте `http://localhost:5000/swagger` и вызовите методы. Или через curl:

```bash
curl http://localhost:5000/api/Book
curl -X PATCH http://localhost:5000/api/Book/2 -H "Content-Type: application/json" -d '{"stockQuantity":0}'
curl -X DELETE http://localhost:5000/api/Author/1     # 409 — у автора есть книги
```

---

## Блок Клиент: как устроен и как повторить по шагам

### Шаг 1. Создать проект

```bash
dotnet new webapp -n Bookstore.WebClient -o BookWebClient
```

Это Razor Pages: каждая страница — пара файлов `Page.cshtml` (разметка) + `Page.cshtml.cs` (класс `PageModel` с кодом). Bootstrap уже подключён шаблоном.

### Шаг 2. Настройки: адрес API и источник данных

Клиент берёт два параметра из конфигурации:

| Ключ | Пример | Зачем |
|---|---|---|
| `Api:BaseUrl` | `http://localhost:5000/` | Адрес API (слэш в конце обязателен) |
| `DataSource:Mode` | `Api` или `Mock` | Источник данных по умолчанию |

Сейчас они заданы в `Properties/launchSettings.json` как переменные окружения (двойное подчёркивание `__` = двоеточие):

```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "Api__BaseUrl": "http://localhost:5000/",
  "DataSource__Mode": "Api"
}
```

Их можно перенести и в `appsettings.json`:

```json
"Api": { "BaseUrl": "http://localhost:5000/" },
"DataSource": { "Mode": "Api" }
```

### Шаг 3. DTO — зеркало API

Клиент и API — разные проекты и не ссылаются друг на друга, поэтому DTO описываются в клиенте ещё раз (`Models/BookDto.cs` и т.д.). **Имена и типы полей должны совпадать с DTO в API**, иначе JSON не разберётся.

```csharp
public class BookDto : IEntityDto     // IEntityDto { int Id } — нужен мок-хранилищу
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AuthorId { get; set; }
    // ...
}
```

### Шаг 4. ApiGateway — единственное место, где идут HTTP-запросы

1. Регистрируем именованный `HttpClient` с адресом API (`Program.cs`):
   ```csharp
   builder.Services.AddHttpClient(ApiClientName, client => client.BaseAddress = apiBaseUri);
   builder.Services.AddScoped<IApiGateWay, ApiGateway>();
   ```
   `IHttpClientFactory` правильно переиспользует соединения — `new HttpClient()` на каждый запрос делать нельзя.

2. Описываем запрос и ответ (`Infrastructure/Model`):
   ```csharp
   public class ApiRequest
   {
       public ApiType ApiType { get; set; } = ApiType.GET;   // GET, POST, PUT, DELETE
       public object? Data { get; set; }                     // тело (для POST/PUT)
       public string? URL { get; set; }                      // "api/Book/5"
       public string? Token { get; set; }                    // JWT на будущее
   }

   public class ApiResponse<T>
   {
       public HttpStatusCode HttpStatusCode { get; set; }
       public bool IsSuccess { get; set; }
       public string? ErrorMessage { get; set; }
       public T? Result { get; set; }
   }
   ```

3. Шлюз (`Infrastructure/ApiGateway.cs`) — собирает `HttpRequestMessage`, отправляет, разбирает JSON и **никогда не бросает исключение наружу**:
   ```csharp
   public async Task<ApiResponse<T>> ApiRequest<T>(ApiRequest request, CancellationToken ct)
   {
       try
       {
           var client = _httpClientFactory.CreateClient(ApiClientName);
           var message = new HttpRequestMessage
           {
               RequestUri = new Uri(request.URL ?? "", UriKind.RelativeOrAbsolute), // склеится с BaseAddress
               Method = request.ApiType switch { ApiType.POST => HttpMethod.Post, ApiType.PUT => HttpMethod.Put,
                                                 ApiType.DELETE => HttpMethod.Delete, _ => HttpMethod.Get }
           };
           if (request.Data != null) message.Content = JsonContent.Create(request.Data, options: JsonOptions);

           var response = await client.SendAsync(message, ct);
           if (!response.IsSuccessStatusCode)
               return Fail<T>(response.StatusCode, await response.Content.ReadAsStringAsync(ct));

           if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
               return new ApiResponse<T> { IsSuccess = true, HttpStatusCode = response.StatusCode };

           var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
           return new ApiResponse<T> { IsSuccess = true, HttpStatusCode = response.StatusCode, Result = data };
       }
       catch (HttpRequestException ex) { return Fail<T>(0, ex.Message); }      // API не запущен
       catch (TaskCanceledException)   { return Fail<T>(HttpStatusCode.RequestTimeout, "Перевищено час очікування"); }
       catch (JsonException ex)        { return Fail<T>(0, $"Не вдалося розібрати відповідь: {ex.Message}"); }
   }
   ```
   Страница просто проверяет `response.IsSuccess` — без `try/catch`.

### Шаг 5. Сервисы сущностей — CRUD поверх шлюза

Все запросы одинаковые, отличается только адрес — поэтому один базовый класс (`Service/BaseApiService.cs`):

```csharp
public abstract class BaseApiService<T> : IBaseApiService<T> where T : class
{
    private readonly IApiGateWay _gateway;
    private readonly string _endpoint;          // "api/Book"

    protected BaseApiService(IApiGateWay gateway, string endpoint) { _gateway = gateway; _endpoint = endpoint; }

    public Task<ApiResponse<List<T>>> GetAllAsync(CancellationToken ct = default) =>
        _gateway.ApiRequest<List<T>>(new ApiRequest { ApiType = ApiType.GET, URL = _endpoint }, ct);

    public Task<ApiResponse<T>> CreateAsync(T entity, CancellationToken ct = default) =>
        _gateway.ApiRequest<T>(new ApiRequest { ApiType = ApiType.POST, URL = _endpoint, Data = entity }, ct);

    // GetByIdAsync, UpdateAsync (PUT), DeleteAsync — аналогично, URL = $"{_endpoint}/{id}"
}
```

Наследник — три строки:

```csharp
public class BookService : BaseApiService<BookDto>
{
    public BookService(IApiGateWay gateway) : base(gateway, "api/Book") { }
}
```

### Шаг 6. Мок-данные и переключатель «Мок / API»

Чтобы магазин работал без API и базы, сделан второй источник данных с тем же интерфейсом.

1. **`Service/Mock/MockSeedData.cs`** — начальный каталог (12 книг).
2. **`Service/Mock/MockDataStore.cs`** — «база в памяти» (Singleton, одна на всё приложение).
3. **`Service/Mock/MockApiService<T>`** — реализация `IBaseApiService<T>` поверх хранилища. Отвечает теми же кодами, что и API (200, 201, 204, 404).
4. **`Infrastructure/DataSource.cs`** — переключатель. Режим хранится в сессии посетителя, по умолчанию берётся из `DataSource:Mode`.
5. **`Service/SwitchableApiService<T>`** — «стрелочник» (паттерн Proxy): на каждый вызов смотрит на режим и передаёт запрос нужной реализации:
   ```csharp
   private IBaseApiService<T> Current => _switch.Mode == DataSourceMode.Mock ? _mock : _api;

   public Task<ApiResponse<List<T>>> GetAllAsync(CancellationToken ct = default) => Current.GetAllAsync(ct);
   // ... остальные методы так же
   ```
6. Регистрация (`Program.cs`) — страницы получают `IBaseApiService<BookDto>` и не знают, откуда данные:
   ```csharp
   builder.Services.AddScoped<IDataSourceSwitch, SessionDataSourceSwitch>();
   builder.Services.AddSingleton<MockDataStore>();
   builder.Services.AddScoped(typeof(MockApiService<>));

   AddSwitchable<BookDto, BookService>(builder.Services);   // и для Author, Genre, Publisher

   static void AddSwitchable<TDto, TApiService>(IServiceCollection services)
       where TDto : class, IEntityDto where TApiService : BaseApiService<TDto>
   {
       services.AddScoped<TApiService>();
       services.AddScoped<BaseApiService<TDto>>(sp => sp.GetRequiredService<TApiService>());
       services.AddScoped<IBaseApiService<TDto>, SwitchableApiService<TDto>>();
   }
   ```
7. Кнопки «Мок / API» в шапке (`_Layout.cshtml`) отправляют POST на `Pages/DataSource.cshtml.cs`, который меняет режим и возвращает на ту же страницу.

### Шаг 7. CatalogService — витрина

API отдаёт книгу только с `AuthorId`, а покупателю нужно имя автора. `Service/CatalogService.cs` делает **4 запроса параллельно** (книги, авторы, жанры, издательства) и собирает `BookCard` с именами:

```csharp
var booksTask = _books.GetAllAsync(ct);
var authorsTask = _authors.GetAllAsync(ct);
var genresTask = _genres.GetAllAsync(ct);
var publishersTask = _publishers.GetAllAsync(ct);
await Task.WhenAll(booksTask, authorsTask, genresTask, publishersTask);   // ждём все сразу

var authorNames = authors.ToDictionary(a => a.Id, a => a.Name);          // Id -> имя
var cards = books.Select(b => new BookCard
{
    Id = b.Id, Title = b.Title, Price = b.Price,
    AuthorName = authorNames.GetValueOrDefault(b.AuthorId, "Невідомий автор"),
    // ...
}).ToList();
```

Если API не ответил — `CatalogData.ErrorMessage`, и страница предлагает переключиться на мок.

### Шаг 8. Корзина — в сессии

API корзины пока нет, поэтому `Service/SessionCartService.cs` хранит её в сессии (данные на сервере, у браузера — только cookie с номером сессии). Сессия хранит строки, поэтому список превращается в JSON:

```csharp
public List<CartItem> GetItems()
{
    var json = Session.GetString("Cart");
    return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
}

private void Save(List<CartItem> items) => Session.SetString("Cart", JsonSerializer.Serialize(items));
```

Подключение (`Program.cs`): `AddDistributedMemoryCache()`, `AddSession(...)`, `AddHttpContextAccessor()` и `app.UseSession()` в конвейере.

### Шаг 9. Вход и регистрация — cookie-аутентификация

Авторизации в API пока нет, поэтому пользователи хранятся в памяти клиента (`Service/InMemoryUserStore.cs`, пароли — в виде хеша через `PasswordHasher`).

1. Настройка (`Program.cs`):
   ```csharp
   builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/AccessDenied"; });

   builder.Services.AddAuthorization(o =>
       o.AddPolicy("AdminOnly", p => p.RequireRole(ShopRoles.Admin)));

   builder.Services.AddRazorPages(o =>
   {
       o.Conventions.AuthorizeFolder("/Checkout");            // оформление — только после входа
       o.Conventions.AuthorizePage("/Account/Orders");
       o.Conventions.AuthorizeFolder("/Admin", "AdminOnly");  // админка — только Admin
   });
   ```
2. Вход (`Pages/Account/SignInHelper.cs`) — данные пользователя упаковываются в claims и шифруются в cookie:
   ```csharp
   var claims = new List<Claim>
   {
       new(ClaimTypes.Name, user.Name),
       new(ClaimTypes.Email, user.Email),
       new(ClaimTypes.Role, user.Role)
   };
   var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
   await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
   ```
3. В конвейере порядок важен: `UseSession()` → `UseAuthentication()` → `UseAuthorization()`.

### Шаг 10. Оплата — эмуляция

`Service/PaymentService.cs` ведёт себя как платёжный шлюз, но без реальных денег:

1. Проверяет данные: сумма > 0, номер карты по **алгоритму Луна**, срок действия, CVV.
2. Ждёт 1–3 секунды («ответ банка»).
3. Отвечает по тестовым картам (см. [демо-данные](#демо-данные-для-проверки)).
4. Полный номер карты нигде не сохраняется — только последние 4 цифры.

Заказы после оплаты сохраняются в `Service/InMemoryOrderStore.cs` (в памяти).

### Шаг 11. Страницы

| Адрес | Файл | Что делает |
|---|---|---|
| `/` | `Pages/Index` | Каталог: поиск, фильтры (жанр, автор, наличие), сортировка, «До кошика» |
| `/Books/Details/{id}` | `Pages/Books/Details` | Страница книги, «Ще в автора» |
| `/Cart` | `Pages/Cart/Index` | Кошик: −/+, удаление, итог |
| `/Checkout` | `Pages/Checkout/Index` | Оформление и оплата (нужен вход) |
| `/Checkout/Success/{номер}` | `Pages/Checkout/Success` | «Дякуємо за замовлення» |
| `/Account/Login`, `/Register` | `Pages/Account/...` | Вход, регистрация |
| `/Account/Orders` | `Pages/Account/Orders` | Мої замовлення |
| `/Admin/Books` | `Pages/Admin/Books/Index`, `Edit` | Керування книгами (роль Admin) |

Общие части: `Pages/Shared/_Layout.cshtml` (шапка, переключатель источника, корзина, меню пользователя, подвал), `_BookCover.cshtml` (обложка: картинка или нарисованная CSS-обложка), стили — `wwwroot/css/site.css`.

**Приёмы, которые используются на всех страницах:**

- **Post/Redirect/Get** — после POST-формы делаем `Redirect`, чтобы обновление страницы не отправило форму повторно:
  ```csharp
  public async Task<IActionResult> OnPostAddToCartAsync(int bookId, string? returnUrl, CancellationToken ct)
  {
      var book = await _catalog.GetBookAsync(bookId, ct);
      if (book != null && book.InStock)
      {
          _cart.Add(book);
          TempData["Success"] = $"«{book.Title}» додано до кошика.";   // сообщение переживёт редирект
      }
      return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");  // защита от чужих адресов
  }
  ```
- **Проверка форм** — атрибуты (`[Required(ErrorMessage = "...")]`) на модели ввода + `ModelState.IsValid` на сервере + `_ValidationScriptsPartial` для проверки в браузере.
- **Цены** — через `Infrastructure/Formatting.cs`: `price.ToUah()` → `«1 290,5 ₴»`.

### Шаг 12. Program.cs клиента — порядок конвейера

```csharp
app.UseRequestLocalization(...);   // числа из форм разбираются с точкой (690.50)
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();                  // корзина
app.UseAuthentication();           // кто пользователь (cookie)
app.UseAuthorization();            // есть ли права (правила из AddRazorPages)
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.Run();
```

---

## Сквозные примеры: путь запроса от браузера до базы

### Пример 1. Открыть каталог (режим API)

1. Браузер → `GET http://localhost:5001/`.
2. ASP.NET Core находит страницу `Pages/Index.cshtml` и вызывает `IndexModel.OnGetAsync`.
3. `IndexModel` вызывает `CatalogService.GetCatalogAsync`.
4. `CatalogService` вызывает `IBaseApiService<BookDto>.GetAllAsync` (и для авторов, жанров, издательств).
5. DI подставил `SwitchableApiService<BookDto>`; режим `Api` → передаёт в клиентский `BookService`.
6. `BookService` собирает `ApiRequest { GET, "api/Book" }` → `ApiGateway`.
7. `ApiGateway` берёт `HttpClient` с `BaseAddress = http://localhost:5000/` → `GET http://localhost:5000/api/Book`.
8. В API: `BookController.GetAll` → `BookService.GetAllAsync` → `BookRepository.GetAllAsync` → `AppDbContext.Books` → SQL `SELECT` в SQL Server.
9. Обратно: Entity → (AutoMapper) → `BookDTO` → JSON → `200 OK`.
10. `ApiGateway` разбирает JSON в `List<BookDto>` → `ApiResponse { IsSuccess = true, Result = ... }`.
11. `CatalogService` подставляет имена авторов → `List<BookCard>`.
12. `IndexModel` применяет фильтры и сортировку → `Index.cshtml` рисует карточки → HTML уходит в браузер.

### Пример 2. Добавить книгу в корзину

1. Кнопка «До кошика» → форма `POST /?handler=AddToCart` (с антифорджери-токеном).
2. `IndexModel.OnPostAddToCartAsync` → `CatalogService.GetBookAsync(id)` (актуальная цена и остаток).
3. `SessionCartService.Add` → корзина в сессии (JSON).
4. `TempData["Success"]` + `Redirect` → браузер заново открывает каталог, `_Layout` показывает сообщение и счётчик корзины.

### Пример 3. Оформить и оплатить заказ

1. `/Checkout` — правило `AuthorizeFolder("/Checkout")`: гостя перенаправит на `/Account/Login?ReturnUrl=/Checkout`.
2. После входа (cookie) — форма оформления. POST → `CheckoutInput` проверяется атрибутами.
3. `PaymentService.PayAsync` — проверка карты, пауза, результат.
4. Успех → `InMemoryOrderStore.Add(order)` → `SessionCartService.Clear()` → `Redirect` на `/Checkout/Success/{номер}`.
5. Отказ → `ModelState.AddModelError` → форма показывается снова с текстом ошибки.

### Пример 4. Удалить автора, у которого есть книги

1. `DELETE /api/Author/1` → `AuthorController.Delete`.
2. `AuthorService.DeleteAsync` → `AuthorRepository.DeleteAsync`.
3. Repository: автор найден, но `Books.AnyAsync(b => b.AuthorId == 1)` → `DeleteResult.InUse`.
4. Controller: `InUse` → `409 Conflict` с текстом «Неможливо видалити автора: у нього є книги.»

---

## Демо-данные для проверки

**Аккаунты** (хранятся в памяти клиента):

| Роль | Email | Пароль |
|---|---|---|
| Покупатель | `demo@bookstore.local` | `demo123` |
| Администратор | `admin@bookstore.local` | `admin123` |

**Тестовые карты** (срок — любой будущий, CVV — любые 3 цифры):

| Карта | Результат |
|---|---|
| `4242 4242 4242 4242` | Оплата проходит |
| `4000 0000 0000 0002` | Банк отклонил платёж |
| `4000 0000 0000 9995` | Недостаточно средств |

---

## Что ещё не сделано

- **Авторизация в API** (JWT): сейчас пользователи живут только в памяти клиента.
- **Корзина и заказы в API**: сейчас корзина — в сессии клиента, заказы — в памяти (пропадают при перезапуске); остаток книг после покупки не уменьшается.
- **Админка** умеет только книги; авторов, жанры и издательства через интерфейс не добавить.
- **Поиск и постраничный вывод на стороне API** — сейчас фильтрация идёт в памяти клиента.
- **Bookstore.Logger и Bookstore.Notyfiler** — заготовки; RabbitMQ не подключён (план — в `TODO.md`).
- **Тестов нет.**
