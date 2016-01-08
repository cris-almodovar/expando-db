using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public class ErrorResponseDto
    {
        public DateTime timestamp { get; set; }
        public HttpStatusCode statusCode { get; set; }
        public string message { get; set; }
    }
}
