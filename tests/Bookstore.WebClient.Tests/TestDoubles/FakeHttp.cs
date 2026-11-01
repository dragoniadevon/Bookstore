using System.Net;
using System.Text;

namespace Bookstore.WebClient.Tests.TestDoubles
{
    // "Поддельный" HTTP-обработчик: вместо настоящей сети возвращает заранее
    // заданный ответ и запоминает, какой запрос к нему пришёл.
    //
    // Так можно проверить ApiGateway без запущенного API:
    // что он отправил (метод, адрес, заголовки, тело) и как разобрал ответ.
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        // Удобный конструктор: всегда отвечать заданным кодом и JSON-телом
        public static FakeHttpMessageHandler Returns(HttpStatusCode code, string? json = null) =>
            new(_ => new HttpResponseMessage(code)
            {
                Content = json == null ? new ByteArrayContent([]) : new StringContent(json, Encoding.UTF8, "application/json")
            });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond(request);
        }
    }

    // Фабрика HttpClient, которая отдаёт клиент с поддельным обработчиком
    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) =>
            new(_handler, disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }
}
