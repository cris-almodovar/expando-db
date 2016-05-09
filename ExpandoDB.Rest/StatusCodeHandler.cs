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
    /// <summary>
    /// Provides custom handling of specific HTTP status codes
    /// </summary>
    /// <seealso cref="Nancy.ErrorHandling.IStatusCodeHandler" />
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

        /// <summary>
        /// Check if the error handler can handle errors of the provided status code.
        /// </summary>
        /// <param name="statusCode">Status code</param>
        /// <param name="context">The <see cref="T:Nancy.NancyContext" /> instance of the current request.</param>
        /// <returns>
        /// True if handled, false otherwise
        /// </returns>
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            // We only override the 404 (Not Found) status code.
            return statusCode == HttpStatusCode.NotFound;
        }
    }
}
