using Nancy.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using ExpandoDB.Rest.DTO;
using Nancy.Responses;

namespace ExpandoDB.Rest
{
    public class StatusCodeHandler : IStatusCodeHandler
    {
        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            if (statusCode == HttpStatusCode.NotFound)
            {
                var dto = new ErrorResponseDto { timestamp = DateTime.UtcNow, errorMessage = "The resource you have requested cannot be found.", statusCode = HttpStatusCode.NotFound };
                var response = new JsonResponse<ErrorResponseDto>(dto, new DefaultJsonSerializer())
                {
                    StatusCode = HttpStatusCode.NotFound
                };

                context.Response = response;
            }
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.NotFound;
        }
    }
}
