using Bookstore.WebClient.Infrastructure;
using Bookstore.WebClient.Infrastructure.Model;
using Bookstore.WebClient.Models;
using Bookstore.WebClient.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using static Bookstore.WebClient.Infrastructure.Interface.AppConstants;

namespace Bookstore.WebClient.Tests
{
    // Тесты шлюза к API с поддельным HTTP: проверяем, ЧТО отправляется и КАК разбирается ответ.
    public class ApiGatewayTests
    {
        private static ApiGateway CreateGateway(FakeHttpMessageHandler handler, IHttpContextAccessor? accessor = null) =>
            new(new FakeHttpClientFactory(handler), accessor);

        [Fact]
        public async Task Get_ParsesJsonIntoResult()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.OK, """[{"id":1,"name":"Тарас Шевченко"}]""");
            var gateway = CreateGateway(handler);

            var response = await gateway.ApiRequest<List<AuthorDto>>(new ApiRequest { URL = "api/Author" }, default);

            Assert.True(response.IsSuccess);
            Assert.Equal("Тарас Шевченко", Assert.Single(response.Result!).Name);
            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Equal("http://api.test/api/Author", handler.LastRequest.RequestUri!.ToString());
        }

        [Fact]
        public async Task Post_SendsJsonBodyInCamelCase()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.Created, """{"id":5,"name":"Новий"}""");
            var gateway = CreateGateway(handler);

            await gateway.ApiRequest<AuthorDto>(new ApiRequest
            {
                ApiType = ApiType.POST,
                URL = "api/Author",
                Data = new AuthorDto { Name = "Новий" }
            }, default);

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);

            // Разбираем тело как JSON: System.Text.Json экранирует кириллицу (Н...),
            // поэтому сравнивать строку напрямую нельзя. Имя поля — в camelCase ("name").
            using var json = System.Text.Json.JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal("Новий", json.RootElement.GetProperty("name").GetString());
        }

        [Fact]
        public async Task ExplicitToken_IsSentAsBearerHeader()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.OK, "{}");
            var gateway = CreateGateway(handler);

            await gateway.ApiRequest<object>(new ApiRequest { URL = "api/Cart", Token = "abc.def.ghi" }, default);

            Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal("abc.def.ghi", handler.LastRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task TokenFromSignedInUser_IsSentAutomatically()
        {
            // Пользователь вошёл: токен лежит в claim "access_token" его cookie
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AccessTokenClaim, "user-token")], "test"));
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.OK, "{}");
            var gateway = CreateGateway(handler, TestSession.CreateAccessor(user: user));

            await gateway.ApiRequest<object>(new ApiRequest { URL = "api/Cart" }, default);

            Assert.Equal("user-token", handler.LastRequest!.Headers.Authorization!.Parameter);
        }

        [Fact]
        public async Task NoToken_NoAuthorizationHeader()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.OK, "[]");
            var gateway = CreateGateway(handler);

            await gateway.ApiRequest<List<BookDto>>(new ApiRequest { URL = "api/Book" }, default);

            Assert.Null(handler.LastRequest!.Headers.Authorization);
        }

        [Fact]
        public async Task ErrorStatus_ReturnsFailureWithPlainText()
        {
            // API присылает ошибку как JSON-строку: в кавычках и с экранированной кириллицей
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.Conflict, "\"\\u041D\\u0435\\u043C\\u043E\\u0436\\u043B\\u0438\\u0432\\u043E\"");
            var gateway = CreateGateway(handler);

            var response = await gateway.ApiRequest<object>(new ApiRequest { ApiType = ApiType.DELETE, URL = "api/Author/1" }, default);

            Assert.False(response.IsSuccess);
            Assert.Equal(HttpStatusCode.Conflict, response.HttpStatusCode);
            Assert.Equal("Неможливо", response.ErrorMessage);   // без кавычек, обычный текст
        }

        [Fact]
        public async Task Unauthorized_WithToken_ReturnsSessionExpiredMessage()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.Unauthorized);
            var gateway = CreateGateway(handler);

            var response = await gateway.ApiRequest<object>(new ApiRequest { URL = "api/Cart", Token = "expired" }, default);

            Assert.Equal(HttpStatusCode.Unauthorized, response.HttpStatusCode);
            Assert.Equal("Сеанс завершився. Увійдіть знову.", response.ErrorMessage);
        }

        [Fact]
        public async Task NoContent_IsSuccessWithoutResult()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.NoContent);
            var gateway = CreateGateway(handler);

            var response = await gateway.ApiRequest<object>(new ApiRequest { ApiType = ApiType.DELETE, URL = "api/Cart" }, default);

            Assert.True(response.IsSuccess);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task NetworkError_ReturnsFailureWithStatusZero()
        {
            // Обработчик бросает исключение — как будто API не запущен
            var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
            var gateway = CreateGateway(handler);

            var response = await gateway.ApiRequest<object>(new ApiRequest { URL = "api/Book" }, default);

            Assert.False(response.IsSuccess);
            Assert.Equal((HttpStatusCode)0, response.HttpStatusCode);
        }

        [Fact]
        public async Task InvalidJson_ReturnsFailureInsteadOfException()
        {
            var handler = FakeHttpMessageHandler.Returns(HttpStatusCode.OK, "{ це не json");
            var gateway = CreateGateway(handler);

            var response = await gateway.ApiRequest<BookDto>(new ApiRequest { URL = "api/Book/1" }, default);

            Assert.False(response.IsSuccess);
            Assert.StartsWith("Не вдалося розібрати відповідь", response.ErrorMessage);
        }
    }
}
