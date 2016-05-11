using ExpandoDB.Rest.DTO;
using Common.Logging;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Json;
using Nancy.Responses;
using Nancy.TinyIoc;
using System;
using System.Linq;
using Nancy.Responses.Negotiation;
using System.Configuration;
using Nancy.Conventions;
using Metrics;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Configures the NancyFX instrastructure for ExpandoDB.
    /// </summary>
    /// <seealso cref="Nancy.DefaultNancyBootstrapper" />
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(Bootstrapper).Name);

        /// <summary>
        /// Initialise the bootstrapper - can be used for adding pre/post hooks and
        /// any other initialisation tasks that aren't specifically container setup
        /// related
        /// </summary>
        /// <param name="container">Container instance for resolving types if required.</param>
        /// <param name="pipelines"></param>
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            EnableCORS(pipelines);            

            // Configure JSON handling.
            JsonSettings.RetainCasing = true;
            JsonSettings.ISO8601DateFormat = true;
            JsonSettings.MaxJsonLength = Int32.MaxValue;            

            // Configure Metrics.NET             
            Metric.Config                                 
                  .WithAppCounters()                  
                  .WithNancy(pipelines);            

            ConfigureExceptionHandling(pipelines);
        }

        /// <summary>
        /// Configures exception handling.
        /// </summary>
        /// <param name="pipelines">The pipelines.</param>
        private void ConfigureExceptionHandling(IPipelines pipelines)
        {
            StaticConfiguration.DisableErrorTraces = Boolean.Parse(ConfigurationManager.AppSettings["NancyDisableErrorTraces"] ?? "false");

            // Configure exception handling for Web Service endpoints.                        
            pipelines.OnError.AddItemToStartOfPipeline((ctx, ex) =>
            {
                _log.Error(ex);                

                var dto = new ErrorResponseDto
                {
                    timestamp = DateTime.UtcNow,
                    errorMessage = $"{ex.GetType().Name} - {ex.Message}",
                    statusCode = HttpStatusCode.InternalServerError
                };

                var response = new JsonResponse<ErrorResponseDto>(dto, new DefaultJsonSerializer())
                {
                    StatusCode = HttpStatusCode.InternalServerError
                };

                return response;
            }
            );

            // Log all unhandled exceptions.
            AppDomain.CurrentDomain.UnhandledException += (sender, ex) =>
            {
                _log.Error(ex);
            };
        }

        /// <summary>
        /// Enables Cross-Origin requests.
        /// </summary>
        /// <param name="pipelines">The pipelines.</param>
        private static void EnableCORS(IPipelines pipelines)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                var request = ctx.Request;
                var response = ctx.Response;

                if (request.Headers.Keys.Contains("Origin"))
                {
                    var origins = "" + string.Join(" ", request.Headers["Origin"]);
                    response.Headers["Access-Control-Allow-Origin"] = origins;

                    if (request.Method == "OPTIONS")
                    {
                        // Handle CORS preflight request
                        response.Headers["Access-Control-Allow-Methods"] =
                            "GET, POST, PUT, PATCH, DELETE, OPTIONS";

                        if (request.Headers.Keys.Contains("Access-Control-Request-Headers"))
                        {
                            var allowedHeaders = "" + string.Join(
                                ", ", request.Headers["Access-Control-Request-Headers"]);

                            response.Headers["Access-Control-Allow-Headers"] = allowedHeaders;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Configures the container using AutoRegister followed by registration
        /// of default INancyModuleCatalog and IRouteResolver.
        /// </summary>
        /// <param name="container">Container instance</param>
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            Config.LuceneNullToken = ConfigurationManager.AppSettings["LuceneNullToken"] ?? Config.LuceneNullToken;
            Config.DataPath = ConfigurationManager.AppSettings["DataPath"] ?? Config.DataPath;

            // There is only one instance of the Database object in the application.
            // It is created here, and registered with the IOC container so that 
            // it can be auto-injected into DbService instances.
                        
            var database = new Database(Config.DataPath);
            container.Register<Database>(database);                       
        }

        /// <summary>
        /// Overrides Nancy's internal configuration.
        /// </summary>
        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                // ExpandoDB only returns one Content-Type => application/json.
                // Here we only enable the JsonProcessor, and disable all others (HTML, XML, etc.)
                return NancyInternalConfiguration.WithOverrides(
                    c =>
                    {
                        c.ResponseProcessors.Clear();
                        c.ResponseProcessors.Add(typeof(JsonProcessor));
                    }
                );
            }
        }

        /// <summary>
        /// Overrides/configures Nancy's conventions
        /// </summary>
        /// <param name="nancyConventions">Convention object instance</param>
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            // Here we configure the Swagger API directory as a static web directory.
            nancyConventions.StaticContentsConventions.AddDirectory(@"/api-spec");
        }
    }
}
