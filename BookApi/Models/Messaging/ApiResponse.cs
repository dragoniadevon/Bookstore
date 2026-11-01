using System.Net;

namespace Bookstore.Api.Models.Messaging
{
    public class ApiResponse
    {
        public HttpStatusCode Response { get; set; }
        public bool isSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? Result { get; set; }
    }
}