using Bookstore.Api.Models.Entity;
using Bookstore.Api.Models.Entity.Base;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Data
{
    // AppDbContext — основной класс для работы приложения с базой данных.
    //
    // DbContext предоставляет EF Core возможность:
    // - выполнять запросы к БД;
    // - добавлять, изменять и удалять данные;
    // - отслеживать изменения объектов;
    // - сохранять изменения через SaveChangesAsync().
    internal class AppDbContext : DbContext
    {
        // DbContextOptions содержит настройки подключения и работы
        // данного DbContext с базой данных.
        //
        // AppDbContext получает эти настройки через Dependency Injection.
        // base(options) передаёт их в родительский класс DbContext.
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet<Author> представляет таблицу Authors в базе данных.
        //
        // Через это свойство мы можем получать, добавлять,
        // изменять и удалять авторов.
        //
        // Например:
        // _dbContext.Authors.ToListAsync();
        public DbSet<Author> Authors { get; set; }

        // DbSet<Book> представляет таблицу Books в базе данных.
        //
        // Через это свойство мы можем получать, добавлять,
        // изменять и удалять книги.
        public DbSet<Book> Books { get; set; }

        // DbSet<Genre> представляет таблицу Genres в базе данных.
        //
        // Через это свойство мы можем работать с жанрами книг.
        public DbSet<Genre> Genres { get; set; }

        // DbSet<Publisher> представляет таблицу Publishers в базе данных.
        //
        // Через это свойство мы можем работать с издательствами.
        public DbSet<Publisher> Publishers { get; set; }

        // DbSet<User> представляет таблицу Users в базе данных.
        //
        // Через это свойство мы можем работать с пользователями.
        public DbSet<User> Users { get; set; }

        // DbSet<Cart> представляет таблицу Carts в базе данных.
        //
        // Через это свойство мы можем работать с корзинами пользователей.
        public DbSet<Cart> Carts { get; set; }

        // DbSet<CartItem> представляет таблицу CartItems в базе данных.
        //
        // Через это свойство мы можем работать с элементами корзины.
        public DbSet<CartItem> CartItems { get; set; }

        // DbSet<Order> представляет таблицу Orders в базе данных.
        //
        // Через это свойство мы можем работать с заказами.
        public DbSet<Order> Orders { get; set; }

        // DbSet<OrderItem> представляет таблицу OrderItems в базе данных.
        //
        // Через это свойство мы можем работать с элементами заказа.
        public DbSet<OrderItem> OrderItems { get; set; }

        // Этот метод вызывается EF Core при построении модели базы данных.
        //
        // Здесь можно настроить:
        // - связи между сущностями;
        // - ограничения;
        // - индексы;
        // - начальные данные (seed data).
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // HasData добавляет начальные данные в базу данных.
            //
            // Эти данные будут добавлены при применении миграции.
            // В данном случае при создании/обновлении БД
            // будет добавлен один автор (Id = 1).
            //
            // Значения HasData должны быть постоянными (не DateTime.UtcNow),
            // иначе EF Core будет считать, что модель меняется при каждом запуске,
            // и создавать новую миграцию. Поэтому дата задана явно.
            //
            // Остальной демонстрационный каталог добавляет DbSeeder
            // при запуске API в режиме разработки.
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Author>().HasData(
                new Author
                {
                    Id = 1,
                    Name = "Тарас Шевченко",
                    CreatedAt = seedDate
                }
            );

            // Начальные данные: жанр.
            //
            // Нужен, чтобы можно было сразу создавать книги
            // с genreId = 1: BookService проверяет, что жанр существует.
            modelBuilder.Entity<Genre>().HasData(
                new Genre
                {
                    Id = 1,
                    Name = "Класика",
                    CreatedAt = seedDate
                }
            );

            // Начальные данные: издательство.
            //
            // Нужно, чтобы можно было сразу создавать книги
            // с publisherId = 1.
            modelBuilder.Entity<Publisher>().HasData(
                new Publisher
                {
                    Id = 1,
                    Name = "Книжковий дім",
                    Description = "Класична та сучасна проза",
                    CreatedAt = seedDate
                }
            );


            // Цена книги хранится как decimal(18, 2):
            // до 18 цифр всего, из них 2 после запятой (копейки).
            // Без настройки EF Core выдаёт предупреждение
            // и может обрезать дробную часть.
            modelBuilder.Entity<Book>()
                .Property(b => b.Price)
                .HasPrecision(18, 2);

            // То же для денежных полей заказа: общая сумма и цена позиции.
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);


            // Позиции заказа ссылаются на книгу.
            //
            // По умолчанию EF Core настраивает каскадное удаление: удалили книгу —
            // удалились и все строки заказов с ней, то есть пропала бы история покупок.
            // Restrict запрещает удалять книгу, которая есть в заказах
            // (BookRepository заранее проверяет это и возвращает InUse).
            //
            // Для CartItem каскад оставляем: если книгу удалили из магазина,
            // пусть она просто исчезнет из корзин.
            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Book)
                .WithMany()
                .HasForeignKey(i => i.BookId)
                .OnDelete(DeleteBehavior.Restrict);


            // ---------- Пользователи, корзины, заказы ----------

            // Email пользователя уникален: два аккаунта с одним email быть не может.
            // HasIndex(...).IsUnique() создаёт в базе уникальный индекс —
            // база сама не даст вставить дубликат, даже если проверку в коде забыли.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Один пользователь — одна корзина (связь "один к одному").
            // Внешний ключ — Cart.UserId. При удалении пользователя удаляется и корзина.
            modelBuilder.Entity<User>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // В одной корзине одна книга встречается только один раз —
            // если её добавляют повторно, увеличиваем Quantity существующей позиции.
            modelBuilder.Entity<CartItem>()
                .HasIndex(i => new { i.CartId, i.BookId })
                .IsUnique();

            // Номер заказа уникален
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Number)
                .IsUnique();

            // Ограничения длины строк: без них EF Core создаёт nvarchar(max),
            // а по nvarchar(max) нельзя построить индекс (Email, Number).
            modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(200);
            modelBuilder.Entity<User>().Property(u => u.Name).HasMaxLength(100);
            modelBuilder.Entity<User>().Property(u => u.Role).HasMaxLength(20);
            modelBuilder.Entity<Order>().Property(o => o.Number).HasMaxLength(30);
            modelBuilder.Entity<Order>().Property(o => o.Status).HasMaxLength(20);
            modelBuilder.Entity<Order>().Property(o => o.CardLast4).HasMaxLength(4);


            // Связи Book с Author, Genre и Publisher.
            //
            // Один автор / жанр / издательство → много книг.
            // Restrict запрещает удалять автора, жанр или издательство,
            // пока у них есть книги.
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Genre)
                .WithMany(g => g.Books)
                .HasForeignKey(b => b.GenreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Publisher)
                .WithMany(p => p.Books)
                .HasForeignKey(b => b.PublisherId)
                .OnDelete(DeleteBehavior.Restrict);
        }


        // Переопределяем SaveChangesAsync, чтобы автоматически заполнять
        // служебные даты из BaseEntity во ВСЕХ сущностях:
        // - CreatedAt — при добавлении записи;
        // - ModifiedAt — при изменении записи.
        //
        // Так не нужно помнить об этом в каждом репозитории.
        //
        // ChangeTracker — "журнал" EF Core: в нём видно, какие объекты
        // добавлены (Added), изменены (Modified) или удалены (Deleted).
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = now;

                    // CreatedAt при изменении трогать нельзя. Если объект пришёл
                    // через Update(), EF Core пометит изменёнными все поля —
                    // запрещаем перезаписывать дату создания.
                    entry.Property(e => e.CreatedAt).IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}