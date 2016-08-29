using Nancy.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using ExpandoDB.Rest.DTO;
using Nancy.Responses;
using System.Dynamic;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Provides custom handling of specific HTTP status codes
    /// </summary>
    /// <seealso cref="Nancy.ErrorHandling.IStatusCodeHandler" />
    public class StatusCodeHandler : IStatusCodeHandler
    {
        /// <summary>
        /// Handle the error code
        /// </summary>
        /// <param name="statusCode">Status code</param>
        /// <param name="context">Current context</param>
        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            // Special handling for 404 Not Found
            if (statusCode == HttpStatusCode.NotFound)
            {
                dynamic dto = new ExpandoObject();
                dto.statusCode = (int)HttpStatusCode.NotFound;
                dto.errorMessage = "The resource you have requested cannot be found.";                
                dto.timestamp = DateTime.UtcNow;

                var response = new JsonResponse<ExpandoObject>(dto, new DtoSerializer())
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
