# Bookstore — книжный интернет-магазин

Учебный проект на **ASP.NET Core (.NET 10)**: книжный интернет-магазин из двух основных частей.

- **Bookstore.Api** — серверная часть (REST API). Хранит в SQL Server каталог, пользователей, корзины и заказы и отдаёт их по HTTP в формате JSON. Вход — через **JWT-токены**, изменять каталог может только администратор, при оформлении заказа уменьшается остаток на складе.
- **Bookstore.WebClient** — сайт магазина (Razor Pages). Показывает каталог, корзину, оформление заказа с оплатой, вход и регистрацию, админку для книг. С базой напрямую **не работает** — все данные получает из API по HTTP.
- **tests/** — автоматические тесты: интеграционные для API и unit-тесты для клиента (см. [Тесты](#тесты)).

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
7. [Тесты](#тесты)
8. [Демо-данные для проверки](#демо-данные-для-проверки)
9. [Что ещё не сделано](#что-ещё-не-сделано)

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
│   ├── Controllers/               — каталог (Author, Book, Genre, Publisher), Auth, Cart, Order
│   ├── Data/                      — AppDbContext (база), DbSeeder (демо-каталог и демо-пользователи)
│   ├── Migrations/                — миграции EF Core (создаются командой, руками не правятся)
│   ├── Models/
│   │   ├── Entity/                — сущности = таблицы базы (Book, Author, User, Cart, Order...)
│   │   ├── DTO/                   — объекты для обмена с клиентом (BookDTO, CartDTO, OrderDTO, AuthDTO...)
│   │   ├── DeleteResult.cs        — результат удаления (Deleted / NotFound / InUse)
│   │   ├── ServiceResult.cs       — результат операции сервиса (Ok / NotFound / Conflict ...)
│   │   └── UserRoles.cs           — роли Customer и Admin
│   ├── Security/                  — JWT: настройки, создание токенов, Id пользователя из токена
│   ├── Repositories/              — доступ к базе (каталог)
│   ├── Services/                  — логика: каталог, AuthService, CartService, OrderService
│   ├── MappingConfig.cs           — правила AutoMapper (Entity ↔ DTO)
│   └── Program.cs                 — регистрация сервисов, JWT, запуск
│
├── BookWebClient/                 — Bookstore.WebClient
│   ├── Infrastructure/            — ApiGateway (HTTP к API + токен), переключатель источника, форматирование цен
│   ├── Models/                    — DTO (зеркало API), корзина, заказ, пользователь, модели оплаты
│   │   └── Api/                   — DTO для /api/Auth, /api/Cart, /api/Order
│   ├── Service/                   — сервисы каталога, корзины, входа, заказов (API- и мок-версии), оплата
│   │   └── Mock/                  — мок-данные и мок-реализация API
│   ├── Pages/                     — Razor-страницы (каталог, книга, корзина, оформление, аккаунт, админка)
│   ├── wwwroot/                   — css, js, Bootstrap
│   └── Program.cs
│
├── tests/
│   ├── Bookstore.Api.Tests/       — интеграционные тесты API (настоящий API в памяти + SQLite)
│   └── Bookstore.WebClient.Tests/ — unit-тесты клиента (оплата, шлюз, корзина, мок-хранилище)
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

2. **Ключ для подписи JWT-токенов** (секрет, в репозиторий не коммитится; минимум 32 символа):
   ```bash
   dotnet user-secrets init --project BookApi
   dotnet user-secrets set "Jwt:Key" "придумайте-длинную-случайную-строку-от-32-символов" --project BookApi
   ```
   Если ключ не задать, в режиме Development API создаст временный ключ при запуске (в логе будет предупреждение) — после перезапуска API всем придётся войти заново. На рабочем сервере без ключа API не запустится.

3. **Восстановите инструменты и создайте базу:**
   ```bash
   dotnet tool restore
   dotnet ef database update --project BookApi
   ```

4. **Запустите API** (в отдельном терминале):
   ```bash
   cd BookApi
   dotnet run --launch-profile http
   ```
   API слушает `http://localhost:5000`, Swagger — `http://localhost:5000/swagger`.
   При первом запуске в режиме Development пустая база заполнится демо-каталогом (12 книг) и демо-пользователями (см. [демо-данные](#демо-данные-для-проверки)).

5. **Запустите клиент:**
   ```bash
   cd BookWebClient
   dotnet run --launch-profile http
   ```
   Откройте `http://localhost:5001`.

6. **Запустите тесты** (API и база для них не нужны):
   ```bash
   dotnet test Dragonia.slnx
   ```

> Клиент можно запустить и **без API и базы**: нажмите в шапке сайта кнопку **«Мок»** — каталог, вход, корзина и заказы будут работать в памяти клиента.

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

Эндпоинты каталога (одинаково для `Author`, `Book`, `Genre`, `Publisher`). Читать может кто угодно, изменять — только администратор (подробнее — [шаг 15](#шаг-15-защитить-эндпоинты-ролями)):

| Метод | Адрес | Кто может | Ответ |
|---|---|---|---|
| GET | `/api/Book` | все | 200 + список |
| GET | `/api/Book/{id}` | все | 200 / 404 |
| POST | `/api/Book` | Admin | 201 + объект / 400 / 401 / 403 |
| PUT | `/api/Book/{id}` | Admin | 200 / 404 / 401 / 403 |
| PATCH | `/api/Book/{id}` | Admin | 200 / 404 / 401 / 403 |
| DELETE | `/api/Book/{id}` | Admin | 204 / 404 / 409 / 401 / 403 |

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
curl -X DELETE http://localhost:5000/api/Author/1     # 401 — нужен токен администратора (шаги 14–15)
```

Методы с токеном удобнее проверять в Swagger: выполните `POST /api/Auth/login`, скопируйте `token` из ответа, нажмите кнопку **Authorize** вверху страницы и вставьте его.

### Шаг 14. Пользователи и вход через JWT

**Как работает JWT.** Пользователь один раз отправляет email и пароль, API проверяет их и выдаёт **токен** — строку вида `заголовок.данные.подпись`. В «данных» записано, кто это и какая у него роль, а подпись сделана секретным ключом сервера. Дальше клиент прикладывает токен к каждому запросу (`Authorization: Bearer <токен>`), а API проверяет подпись и срок. Сессий на сервере нет — всё нужное лежит в токене. Подделать токен (например, поменять роль на Admin) нельзя: подпись перестанет сходиться.

1. **Сущность пользователя** (`Models/Entity/User.cs`) — храним **хеш** пароля, а не сам пароль, и роль:
   ```csharp
   public class User : BaseEntity
   {
       public string Name { get; set; } = string.Empty;
       public string Email { get; set; } = string.Empty;        // уникальный индекс в AppDbContext
       public string PasswordHash { get; set; } = string.Empty;
       public string Role { get; set; } = UserRoles.Customer;   // "Customer" или "Admin"
       public Cart? Cart { get; set; }
       public ICollection<Order> Orders { get; set; } = new List<Order>();
   }
   ```
   В `AppDbContext`: `modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();` — база сама не даст создать два аккаунта с одним email.

2. **Настройки JWT** (`Security/JwtSettings.cs`) — ключ берётся из настройки `Jwt:Key` (user-secrets / переменная окружения), никогда не из кода:
   ```csharp
   var key = configuration["Jwt:Key"];
   if (string.IsNullOrWhiteSpace(key))
   {
       if (environment.IsProduction()) throw new InvalidOperationException("Не задан ключ JWT...");
       key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));   // временный ключ для разработки
   }
   SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
   ```

3. **Создание токена** (`Security/TokenService.cs`) — в токен кладём только открытые данные (содержимое токена не зашифровано, только подписано):
   ```csharp
   var descriptor = new SecurityTokenDescriptor
   {
       Issuer = _settings.Issuer,
       Audience = _settings.Audience,
       Expires = DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes),
       Claims = new Dictionary<string, object>
       {
           ["sub"] = user.Id.ToString(), ["name"] = user.Name, ["email"] = user.Email, ["role"] = user.Role
       },
       SigningCredentials = new SigningCredentials(_settings.SigningKey, SecurityAlgorithms.HmacSha256)
   };
   var token = new JsonWebTokenHandler().CreateToken(descriptor);
   ```

4. **Регистрация и вход** (`Services/AuthService.cs`) — пароль хешируется через `PasswordHasher<User>` из ASP.NET Core Identity:
   ```csharp
   // Регистрация
   user.PasswordHash = _hasher.HashPassword(user, dto.Password);

   // Вход: одна и та же ошибка для "нет такого email" и "неверный пароль",
   // чтобы подбором нельзя было узнать, какие email зарегистрированы
   if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
       return ServiceResult<AuthResponseDTO>.Fail(ServiceStatus.Unauthorized, "Неправильний email або пароль.");
   ```

5. **Контроллер** (`Controllers/AuthController.cs`):

   | Метод | Адрес | Ответ |
   |---|---|---|
   | POST | `/api/Auth/register` | 200 + `{ token, expiresAt, user }` / 409 email занят / 400 |
   | POST | `/api/Auth/login` | 200 + `{ token, expiresAt, user }` / 401 |
   | GET | `/api/Auth/me` | 200 + пользователь / 401 без токена |

6. **Подключение в `Program.cs`** — проверка токена на каждом запросе:
   ```csharp
   builder.Services.AddSingleton(sp => JwtSettings.FromConfiguration(
       sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<IHostEnvironment>()));
   builder.Services.AddSingleton<ITokenService, TokenService>();

   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
   builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
       .Configure<JwtSettings>((options, jwt) =>
       {
           options.MapInboundClaims = false;              // оставить короткие имена claims ("sub", "role")
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience,
               IssuerSigningKey = jwt.SigningKey,         // подпись проверяется нашим ключом
               ValidateLifetime = true,                   // просроченный токен не принимается
               NameClaimType = "name", RoleClaimType = "role"
           };
       });

   // ...
   app.UseAuthentication();   // кто это (читает токен)  — ДО авторизации
   app.UseAuthorization();    // есть ли права ([Authorize])
   ```
   `UseHttpsRedirection()` включён только на рабочем сервере: при перенаправлении `HttpClient` не переносит заголовок `Authorization`, и токен терялся бы.

7. **Демо-пользователи** — `DbSeeder.SeedDemoUsersAsync` в режиме Development создаёт покупателя и администратора, если пользователей в базе нет.

### Шаг 15. Защитить эндпоинты ролями

```csharp
[Authorize(Roles = UserRoles.Admin)]   // изменять каталог может только администратор
[HttpPost]
public async Task<IActionResult> CreateAsync([FromBody] BookDTO newBook) { ... }
```

- Нет токена или он неверный/просрочен → **401 Unauthorized**.
- Токен есть, но роль не та → **403 Forbidden**.
- `[Authorize]` на всём контроллере (как у `CartController`) — все его методы только для вошедших.
- Id текущего пользователя берётся **из токена**, а не из запроса — так нельзя подставить чужой Id:
  ```csharp
  public static int GetUserId(this ClaimsPrincipal user) => int.Parse(user.FindFirstValue("sub")!);
  ```

### Шаг 16. Корзина в API

Таблицы `Carts` и `CartItems` (связь «корзина ↔ книги» через `CartItem` с количеством). Уникальные индексы: у пользователя одна корзина, в корзине одна книга встречается один раз.

`Services/CartService.cs` работает с `AppDbContext` напрямую — одна операция затрагивает несколько таблиц сразу. Пример — добавление книги:

```csharp
var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == dto.BookId);
if (book == null) return ServiceResult<CartDTO>.Fail(ServiceStatus.NotFound, "Книгу не знайдено.");

var cart = await LoadCartAsync(userId) ?? CreateCart(userId);           // корзина создаётся при первом добавлении
var item = cart.Items.FirstOrDefault(i => i.BookId == dto.BookId);
var newQuantity = (item?.Quantity ?? 0) + dto.Quantity;

if (newQuantity > book.StockQuantity)                                    // больше, чем на складе, нельзя
    return ServiceResult<CartDTO>.Fail(ServiceStatus.Conflict, StockError(book));
```

`ServiceResult<T>` — результат с причиной неудачи (NotFound, Conflict, BadRequest...). Контроллер превращает его в HTTP-код одной строкой: `return this.ToActionResult(result, cart => Ok(cart));`.

| Метод | Адрес (все — только с токеном) | Ответ |
|---|---|---|
| GET | `/api/Cart` | 200 + `{ items, totalQuantity, totalAmount }` |
| POST | `/api/Cart/items` `{ bookId, quantity }` | 200 / 404 нет книги / 409 мало на складе |
| PUT | `/api/Cart/items/{bookId}` `{ quantity }` | 200 (0 — убрать) / 404 / 409 |
| DELETE | `/api/Cart/items/{bookId}` | 200 |
| DELETE | `/api/Cart` | 204 — очистить |

### Шаг 17. Заказы: транзакция и уменьшение склада

`Services/OrderService.PlaceOrderAsync` оформляет заказ из корзины **в одной транзакции** — либо всё сразу (склад уменьшен, заказ сохранён, корзина очищена), либо ничего:

```csharp
await using var transaction = await _db.Database.BeginTransactionAsync();

foreach (var item in cart.Items)
{
    // Атомарно: UPDATE Books SET StockQuantity = StockQuantity - @qty WHERE Id = @id AND StockQuantity >= @qty
    var updated = await _db.Books
        .Where(b => b.Id == item.BookId && b.StockQuantity >= item.Quantity)
        .ExecuteUpdateAsync(s => s.SetProperty(b => b.StockQuantity, b => b.StockQuantity - item.Quantity));

    if (updated == 0)   // книг не хватило — выходим, транзакция откатится
        return ServiceResult<OrderDTO>.Fail(ServiceStatus.Conflict, CartService.StockError(current));
}

var order = new Order
{
    Number = GenerateNumber(now), UserId = userId, Status = OrderStatuses.Paid, ...,
    // цена "замораживается" на момент покупки
    OrderItems = cart.Items.Select(i => new OrderItem { BookId = i.BookId, Quantity = i.Quantity, UnitPrice = i.Book.Price }).ToList()
};
_db.Orders.Add(order);
_db.CartItems.RemoveRange(cart.Items);   // корзина очищается

await _db.SaveChangesAsync();
await transaction.CommitAsync();
```

**Почему остаток уменьшается одной командой с условием**, а не «прочитать → проверить → записать»: если два покупателя одновременно покупают последнюю книгу, оба прочитали бы «осталась 1» и оба её «купили». Условие `StockQuantity >= @qty` внутри `UPDATE` база проверяет атомарно — второй запрос просто не изменит ни одной строки и получит 409.

**Оплата и данные карты.** Как и в настоящих магазинах, данные карты уходят только в платёжную систему (в учебном проекте — эмуляция `PaymentService` в клиенте). API получает лишь последние 4 цифры и номер транзакции.

| Метод | Адрес (все — только с токеном) | Ответ |
|---|---|---|
| POST | `/api/Order` `{ customerName, phone, address, cardLast4, transactionId }` | 201 + заказ / 400 корзина пуста / 409 мало на складе |
| GET | `/api/Order` | 200 + мои заказы (новые сверху) |
| GET | `/api/Order/{number}` | 200 / 404 (в том числе для чужого заказа) |

После изменения моделей (шаги 14–17) — новая миграция: `dotnet ef migrations add AddAuthCartOrders --project BookApi` и `dotnet ef database update --project BookApi`.

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

           // JWT: явно переданный токен или токен вошедшего пользователя из его cookie (шаг 9)
           var token = request.Token ?? _httpContextAccessor?.HttpContext?.User.FindFirst(AccessTokenClaim)?.Value;
           if (!string.IsNullOrEmpty(token))
               message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

### Шаг 8. Корзина: в API или в сессии

Интерфейс `ICartService` (асинхронный — корзина в API это HTTP-запросы) и две реализации:

| Реализация | Где корзина | Когда используется |
|---|---|---|
| `ApiCartService` | `/api/Cart` в базе | Режим API и пользователь вошёл (есть токен) |
| `SessionCartService` | Сессия на сервере клиента | Мок-режим и гости |

Выбирает `SwitchableCartService`:

```csharp
private ICartService Current =>
    _switch.Mode == DataSourceMode.Api && _httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaim) != null
        ? _api
        : _session;
```

`ApiCartService` просто вызывает API через шлюз; если книг мало, API отвечает 409, и его текст показывается пользователю:

```csharp
public async Task<string?> AddAsync(BookCard book, int quantity = 1, CancellationToken ct = default)
{
    var response = await _gateway.ApiRequest<ApiCartDto>(new ApiRequest
    {
        ApiType = ApiType.POST, URL = "api/Cart/items",
        Data = new AddCartItemRequestDto { BookId = book.Id, Quantity = quantity }
    }, ct);
    return response.IsSuccess ? null : response.ErrorMessage;   // null = всё хорошо, иначе текст ошибки
}
```

`SessionCartService` хранит список как JSON в сессии (данные на сервере, у браузера — только cookie с номером сессии):

```csharp
var json = Session.GetString("Cart");
var items = string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
// ...
Session.SetString("Cart", JsonSerializer.Serialize(items));
```

**Гостевая корзина после входа.** Гость в режиме API набирает книги в сессию; при входе `CartMerger` переносит их в корзину API (с токеном, который только что вернул API) и очищает сессию.

Подключение сессии (`Program.cs`): `AddDistributedMemoryCache()`, `AddSession(...)`, `AddHttpContextAccessor()` и `app.UseSession()` в конвейере.

### Шаг 9. Вход и регистрация: JWT из API в cookie клиента

Вход в браузере — **cookie-аутентификация клиента**, а внутри cookie лежит **JWT-токен от API**:

```
Браузер ──(email, пароль)──► WebClient ──POST /api/Auth/login──► API
                                         ◄── { token, user } ────
Браузер ◄── зашифрованная cookie (имя, роль, access_token) ── WebClient
Браузер ──(cookie)──► WebClient ──Authorization: Bearer <token>──► API
```

1. `IAuthService` с двумя реализациями: `ApiAuthService` (`/api/Auth/login`, `/api/Auth/register`) и `MockAuthService` (пользователи в памяти — `InMemoryUserStore`, мок-режим). Выбирает `SwitchableAuthService` по режиму.

2. `Pages/Account/SignInHelper.cs` кладёт токен в claim `access_token`, а время жизни cookie делает равным сроку токена:
   ```csharp
   var claims = new List<Claim>
   {
       new(ClaimTypes.Name, user.Name),
       new(ClaimTypes.Email, user.Email),
       new(ClaimTypes.Role, user.Role)          // роль из API — по ней работает админка клиента
   };
   if (!string.IsNullOrEmpty(auth.Token)) claims.Add(new Claim(AccessTokenClaim, auth.Token));

   await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
       new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
       new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = auth.ExpiresAt });
   ```
   Cookie зашифрована ASP.NET Core — из браузера токен прочитать нельзя. `ApiGateway` достаёт его на сервере и добавляет к запросам (шаг 4). Админка клиента в режиме API работает с токеном администратора, поэтому API принимает её POST/PUT/DELETE.

3. При переключении «Мок / API» пользователь выходит из аккаунта (`Pages/DataSource.cshtml.cs`): в моке и в API разные пользователи и книги.

4. Настройка cookie и правил доступа (`Program.cs`):
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
5. В конвейере порядок важен: `UseSession()` → `UseAuthentication()` → `UseAuthorization()`.

### Шаг 10. Оплата и оформление заказа

**Эмуляция оплаты** — `Service/PaymentService.cs` ведёт себя как платёжная система, но без реальных денег:

1. Проверяет данные: сумма > 0, номер карты по **алгоритму Луна**, срок действия, CVV.
2. Ждёт 1–3 секунды («ответ банка»).
3. Отвечает по тестовым картам (см. [демо-данные](#демо-данные-для-проверки)).
4. Полный номер карты нигде не сохраняется — только последние 4 цифры.

**Оформление** — `IOrderService` с двумя реализациями (выбирает `SwitchableOrderService`):

- `ApiOrderService` (режим API):
  1. берёт корзину из `/api/Cart` и проверяет остатки;
  2. проводит оплату через `PaymentService`;
  3. отправляет `POST /api/Order` с контактами, последними 4 цифрами карты и номером транзакции — API уменьшает склад, сохраняет заказ и очищает корзину (шаг 17 блока API);
  4. если API ответил 409 (книги закончились, пока шла оплата) — показывает «Замовлення не оформлено, оплату скасовано».
- `MockOrderService` (мок-режим): корзина из сессии, заказы в `InMemoryOrderStore`, остатки уменьшаются в `MockDataStore`.

Страница `Pages/Checkout/Index.cshtml.cs` не знает, какой режим включён:

```csharp
var result = await _orders.PlaceOrderAsync(new CheckoutData { FullName = Input.FullName, CardNumber = Input.CardNumber, ... }, ct);
if (!result.Success)
{
    ModelState.AddModelError(string.Empty, result.Error!);   // покажется над формой
    return Page();
}
return RedirectToPage("/Checkout/Success", new { number = result.Order!.Number });
```

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

### Пример 2. Войти и добавить книгу в корзину (режим API)

1. Гость нажимает «До кошика» → книга попадает в `SessionCartService` (сессия).
2. Вход: `LoginModel.OnPostAsync` → `ApiAuthService.LoginAsync` → `POST /api/Auth/login`.
3. API: `AuthService` находит пользователя по email, проверяет хеш пароля → `TokenService` создаёт JWT → `200 { token, user }`.
4. Клиент: `SignInHelper` создаёт cookie с именем, ролью и `access_token`; `CartMerger` переносит гостевую корзину в API (`POST /api/Cart/items` с токеном).
5. Следующая кнопка «До кошика» → `SwitchableCartService` видит токен → `ApiCartService` → `ApiGateway` добавляет `Authorization: Bearer ...` → `POST /api/Cart/items`.
6. API: `UseAuthentication` проверяет подпись токена → `CartController` берёт Id пользователя из токена → `CartService` проверяет остаток → сохраняет.

### Пример 3. Оформить и оплатить заказ (режим API)

1. `/Checkout` — правило `AuthorizeFolder("/Checkout")`: гостя перенаправит на `/Account/Login?ReturnUrl=/Checkout`.
2. Форма оформления → POST → `CheckoutInput` проверяется атрибутами.
3. `ApiOrderService`: корзина из `/api/Cart` → `PaymentService.PayAsync` (эмуляция банка) → `POST /api/Order` с последними 4 цифрами карты и номером транзакции.
4. API (`OrderService`, одна транзакция): для каждой позиции `UPDATE Books SET StockQuantity = StockQuantity - n WHERE ... AND StockQuantity >= n` → создаёт `Order` и `OrderItem` с ценами на момент покупки → очищает корзину → `COMMIT` → `201`.
5. Клиент → `Redirect` на `/Checkout/Success/{номер}` → страница берёт заказ из `GET /api/Order/{номер}`.
6. Отказ банка или 409 от API → форма показывается снова с текстом ошибки, склад не меняется.

### Пример 4. Удалить автора, у которого есть книги

1. Админка клиента → `DELETE /api/Author/1` с токеном администратора.
2. API: `[Authorize(Roles = "Admin")]` пропускает (роль в токене — Admin) → `AuthorController.Delete`.
3. `AuthorService.DeleteAsync` → `AuthorRepository.DeleteAsync`: автор найден, но `Books.AnyAsync(b => b.AuthorId == 1)` → `DeleteResult.InUse`.
4. Controller: `InUse` → `409 Conflict` с текстом «Неможливо видалити автора: у нього є книги.»

---

## Тесты

В решении два тестовых проекта на **xUnit**:

| Проект | Вид тестов | Что проверяет |
|---|---|---|
| `tests/Bookstore.Api.Tests` | Интеграционные | Настоящий API целиком: HTTP → Controller → Service → база (SQLite в памяти). Вход и роли, каталог, корзина, заказы, склад |
| `tests/Bookstore.WebClient.Tests` | Unit | Отдельные классы клиента без сети и базы: оплата, ApiGateway, корзина в сессии, мок-хранилище, форматирование |

### Как запустить

```bash
dotnet test Dragonia.slnx                                         # все тесты
dotnet test tests/Bookstore.Api.Tests                             # только тесты API
dotnet test --filter "FullyQualifiedName~OrderTests"              # один класс
dotnet test --filter "FullyQualifiedName~PlaceOrder_EmptyCart"    # один тест
```

В Visual Studio: **Тест → Обозреватель тестов** (Test Explorer) → «Запустить все».

> Если API или клиент сейчас запущены в отладке Visual Studio, их `.exe` заблокированы, и сборка в `Debug` не пройдёт. Запускайте тесты в другой конфигурации: `dotnet test Dragonia.slnx -c Release`.

Ни SQL Server, ни запущенный API для тестов не нужны.

### Как устроен тест: Arrange / Act / Assert

Каждый тест — метод с атрибутом `[Fact]` из трёх частей:

```csharp
[Fact]
public async Task AddItem_MoreThanStock_Returns409()
{
    // Arrange — подготовить данные
    var book = await CreateBookAsync(stock: 2);
    var (client, _) = await RegisterCustomerAsync();

    // Act — выполнить проверяемое действие
    var response = await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id, Quantity = 3 });

    // Assert — проверить результат
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

Имя теста описывает сценарий и ожидаемый результат: `ЧтоПроверяем_ПриКакихУсловиях_ЧтоОжидаем`.

Один тест с разными данными — `[Theory]` + `[InlineData]`:

```csharp
[Theory]
[InlineData("4000 0000 0000 0002", PaymentStatus.Declined)]
[InlineData("4000 0000 0000 9995", PaymentStatus.InsufficientFunds)]
public async Task TestCards_ReturnExpectedStatus(string card, PaymentStatus expected) { ... }
```

### Интеграционные тесты API — как это работает

1. **`Infrastructure/ApiFactory.cs`** — `WebApplicationFactory<Program>` запускает настоящий `Program.cs` API в памяти теста и даёт `HttpClient`, который ходит в него без сети. Отличия от обычного запуска:
   ```csharp
   builder.UseEnvironment("Testing");                                  // DbSeeder не запускается
   builder.ConfigureAppConfiguration((_, config) =>
       config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "test-signing-key-..." }));

   builder.ConfigureServices(services =>
   {
       // Вместо SQL Server — SQLite в памяти
       services.RemoveAll<DbContextOptions<AppDbContext>>();
       services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
       services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
   });
   ```
   В `CreateHost` создаются таблицы (`EnsureCreated`) и тестовый администратор.
2. **`Infrastructure/ApiTestBase.cs`** — помощники: `RegisterCustomerAsync()` (новый покупатель с уникальным email), `CreateAdminClientAsync()`, `CreateBookAsync(stock, price)`, `CreateClient(token)`.
3. **`IClassFixture<ApiFactory>`** — одно приложение и одна база на тестовый класс. Поэтому каждый тест создаёт **свои** данные (своего пользователя, свою книгу) и не зависит от порядка выполнения.
4. Чтобы `internal`-классы API (`AppDbContext`) были видны тестам, в `Bookstore.Api.csproj` добавлено `<InternalsVisibleTo Include="Bookstore.Api.Tests" />`.

### Unit-тесты клиента — как это работает

Unit-тест проверяет один класс, а всё, что ему нужно извне, заменяется **тестовыми заменителями** (`TestDoubles/`):

- `FakeHttpMessageHandler` — «поддельная сеть»: возвращает заданный ответ и запоминает отправленный запрос. Так `ApiGateway` проверяется без API:
  ```csharp
  var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.OK, "{}");
  var gateway = new ApiGateway(new FakeHttpClientFactory(handler));

  await gateway.ApiRequest<object>(new ApiRequest { URL = "api/Cart", Token = "abc.def.ghi" }, default);

  Assert.Equal("abc.def.ghi", handler.LastRequest!.Headers.Authorization!.Parameter);
  ```
- `TestSession` — сессия в памяти для `SessionCartService`.
- `PaymentService` создаётся через `internal`-конструктор без паузы «ответа банка» (`simulateBankDelay: false`), чтобы тесты шли мгновенно (`InternalsVisibleTo` в `Bookstore.WebClient.csproj`).

### Как добавить новый тест — по шагам

**Новый тест API** (например, «нельзя поставить в корзину отрицательное количество»):

1. Откройте подходящий класс (`CartTests.cs`) или создайте новый: `public class MyTests : ApiTestBase { public MyTests(ApiFactory f) : base(f) { } }`.
2. Добавьте метод с `[Fact]`, подготовьте данные через помощники `ApiTestBase`.
3. Отправьте запрос через `HttpClient` (`GetAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`, `PatchAsJsonAsync`, `DeleteAsync`).
4. Проверьте код ответа и, при необходимости, данные (`ReadFromJsonAsync<T>()`) и последствия (например, остаток книги через `GetBookAsync`).
5. `dotnet test --filter "FullyQualifiedName~ИмяТеста"`.

```csharp
[Fact]
public async Task SetQuantity_Negative_Returns400()
{
    var book = await CreateBookAsync(stock: 5);
    var (client, _) = await RegisterCustomerAsync();
    await client.PostAsJsonAsync("api/Cart/items", new AddCartItemDTO { BookId = book.Id });

    var response = await client.PutAsJsonAsync($"api/Cart/items/{book.Id}", new UpdateCartItemDTO { Quantity = -1 });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // [Range(0, 1000)] в UpdateCartItemDTO
}
```

**Новый unit-тест клиента:**

1. Создайте класс в `tests/Bookstore.WebClient.Tests`.
2. Создайте проверяемый объект; внешние зависимости замените заменителями (`FakeHttpMessageHandler`, `TestSession.CreateAccessor()`, `NullLogger<T>.Instance`).
3. Вызовите метод и проверьте результат через `Assert`.

**Новый тестовый проект с нуля** (как были созданы эти):

```bash
dotnet new xunit -n Bookstore.Api.Tests -o tests/Bookstore.Api.Tests
cd tests/Bookstore.Api.Tests
dotnet add reference ../../BookApi/Bookstore.Api.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
cd ../..
dotnet sln Dragonia.slnx add tests/Bookstore.Api.Tests/Bookstore.Api.Tests.csproj
```

Для `WebApplicationFactory<Program>` класс `Program` в API должен быть доступен тестам — поэтому в `BookApi/Program.cs` он объявлен как `public partial class Program`.

### Что покрыто тестами

| Класс | Сценарии |
|---|---|
| `AuthTests` | Регистрация, дубликат email (в том числе в другом регистре), короткий пароль, неверный пароль, `/me` без токена, с токеном и с поддельным токеном |
| `CatalogTests` | Чтение без входа; создание книги гостем (401), покупателем (403), админом (201); Id от клиента игнорируется; удаление автора с книгами (409); PATCH только остатка |
| `CartTests` | 401 без токена, пустая корзина, суммирование количества и итогов, больше чем на складе (409), нет книги (404), количество 0, очистка, корзины разных пользователей не пересекаются |
| `OrderTests` | Склад уменьшается и корзина очищается; пустая корзина (400); остаток упал после добавления (409, ничего не меняется); цена в заказе не меняется после изменения цены книги; только свои заказы; чужой заказ (404); нельзя удалить купленную книгу (409) |
| `PaymentServiceTests` | Успешная оплата, тестовые карты отказа, алгоритм Луна, просроченная карта, CVV, нулевая сумма, год из двух цифр |
| `ApiGatewayTests` | Разбор JSON, тело POST, токен явный и из cookie, без токена, ошибки API, 401, 204, обрыв сети, неверный JSON |
| `SessionCartServiceTests`, `MockApiServiceTests`, `ApiMappingTests`, `FormattingTests` | Корзина в сессии, мок-хранилище, преобразование ответов API, формат гривен и окончаний |

---

## Демо-данные для проверки

**Аккаунты** — одинаковые в обоих режимах: в режиме API их создаёт `DbSeeder` в базе (при первом запуске в Development), в мок-режиме они встроены в `InMemoryUserStore`:

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

- **Обновление токена** (refresh token): когда JWT истекает (по умолчанию через 120 минут), нужно войти заново.
- **Возврат денег при неудачном заказе**: если после оплаты API ответил 409, в эмуляции просто показывается «оплату скасовано» — настоящего возврата нет.
- **Админка** умеет только книги; авторов, жанры и издательства через интерфейс не добавить. Нет просмотра всех заказов для администратора.
- **Управление пользователями**: назначить роль Admin можно только в базе (через регистрацию становятся покупателями).
- **Поиск и постраничный вывод на стороне API** — сейчас фильтрация идёт в памяти клиента.
- **Bookstore.Logger и Bookstore.Notyfiler** — заготовки; RabbitMQ не подключён (план — в `TODO.md`).
