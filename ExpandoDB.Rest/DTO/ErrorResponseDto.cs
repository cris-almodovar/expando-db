using Nancy;
using System;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned when an error occurs in a REST endpoint.
    /// </summary>
    public class ErrorResponseDto
    {
        public DateTime timestamp { get; set; }
        public HttpStatusCode statusCode { get; set; }
        public string errorMessage { get; set; }
    }
}
