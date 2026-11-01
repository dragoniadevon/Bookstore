namespace Bookstore.WebClient.Models
{
    // Данные для частичного представления _BookCover.
    //
    // record — короткая запись класса для "просто данных":
    // компилятор сам создаёт конструктор и свойства.
    // Size: "" (обычная), "cover-sm" (миниатюра в корзине), "cover-lg" (страница книги).
    public record BookCoverModel(int Id, string Title, string Author, string? ImageUrl, string SizeClass = "");
}
