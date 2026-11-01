using System.Globalization;

namespace Bookstore.WebClient.Infrastructure
{
    // Помощники для красивого вывода значений на страницах.
    public static class Formatting
    {
        // Украинский формат чисел: пробел между тысячами, запятая перед копейками
        private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk-UA");

        // Цена в гривнах: 1290.5m -> "1 290,5 ₴". Метод расширения: вызывается как price.ToUah().
        public static string ToUah(this decimal value) => value.ToString("#,0.##", Uk) + " ₴";

        // Правильное окончание слова после числа: 1 книга, 2 книги, 5 книг.
        public static string Plural(this int count, string one, string few, string many)
        {
            var n = Math.Abs(count) % 100;
            if (n is >= 11 and <= 14) return $"{count} {many}";
            return (n % 10) switch
            {
                1 => $"{count} {one}",
                2 or 3 or 4 => $"{count} {few}",
                _ => $"{count} {many}"
            };
        }
    }
}
