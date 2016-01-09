using Nancy;
using System;

namespace ExpandoDB.Rest.DTO
{
    public class ErrorResponseDto
    {
        public DateTime timestamp { get; set; }
        public HttpStatusCode statusCode { get; set; }
        public string message { get; set; }
    }
}
