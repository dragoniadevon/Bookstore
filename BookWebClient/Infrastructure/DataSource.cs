namespace Bookstore.WebClient.Infrastructure
{
    // Откуда магазин берёт данные о книгах, авторах, жанрах и издательствах.
    public enum DataSourceMode
    {
        Mock, // мок-данные в памяти WebClient — работает без API и базы
        Api   // настоящие данные из Bookstore.Api
    }

    // Переключатель источника данных.
    public interface IDataSourceSwitch
    {
        DataSourceMode Mode { get; }
        void SetMode(DataSourceMode mode);
    }

    // Режим хранится в сессии: каждый посетитель может переключить его для себя
    // кнопкой в шапке сайта. Пока посетитель ничего не выбрал, используется
    // значение из настроек "DataSource:Mode" (Mock или Api), а если его нет — Api.
    public class SessionDataSourceSwitch : IDataSourceSwitch
    {
        private const string SessionKey = "DataSourceMode";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataSourceMode _defaultMode;

        public SessionDataSourceSwitch(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;

            // Enum.TryParse превращает строку "Api" в DataSourceMode.Api.
            // ignoreCase: true — подойдёт и "api", и "API".
            _defaultMode = Enum.TryParse(configuration["DataSource:Mode"], ignoreCase: true, out DataSourceMode mode)
                ? mode
                : DataSourceMode.Api;
        }

        public DataSourceMode Mode
        {
            get
            {
                var saved = _httpContextAccessor.HttpContext?.Session.GetString(SessionKey);
                return Enum.TryParse(saved, out DataSourceMode mode) ? mode : _defaultMode;
            }
        }

        public void SetMode(DataSourceMode mode) =>
            _httpContextAccessor.HttpContext?.Session.SetString(SessionKey, mode.ToString());
    }
}
