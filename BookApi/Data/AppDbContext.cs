using Bookstore.Api.Models;
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
            // будет добавлен один тестовый автор.
            modelBuilder.Entity<Author>().HasData(
                new Author
                {
                   
                    Name = "Author",
                    Id = 1
                }
            );


        }
    }
}