using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace Bookstore.WebClient.Tests.TestDoubles
{
    // Сессия в памяти для тестов — простая замена настоящей сессии ASP.NET Core.
    // ISession — это по сути словарь "ключ -> байты".
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _data.Keys;

        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _data.TryGetValue(key, out value);

        // HttpContextAccessor с этой сессией (и, при желании, с вошедшим пользователем)
        public static IHttpContextAccessor CreateAccessor(TestSession? session = null, System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var context = new DefaultHttpContext { Session = session ?? new TestSession() };
            if (user != null) context.User = user;
            return new HttpContextAccessor { HttpContext = context };
        }
    }
}
