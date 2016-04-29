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

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Configures the NancyFX instrastructure for ExpandoDB.
    /// </summary>
    /// <seealso cref="Nancy.DefaultNancyBootstrapper" />
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(Bootstrapper).Name);              

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            EnableCORS(pipelines);
            ConfigureExceptionHandling(pipelines);

            // Configure JSON handling.
            JsonSettings.RetainCasing = true;
            JsonSettings.ISO8601DateFormat = true;
            JsonSettings.MaxJsonLength = Int32.MaxValue;
        }

        private void ConfigureExceptionHandling(IPipelines pipelines)
        {
            StaticConfiguration.DisableErrorTraces = Boolean.Parse(ConfigurationManager.AppSettings["NancyDisableErrorTraces"] ?? "false");

            // Configure exception handling for Web Service endpoints.
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
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

        private static void EnableCORS(IPipelines pipelines)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                if (ctx.Request.Headers.Keys.Contains("Origin"))
                {
                    var origins = "" + string.Join(" ", ctx.Request.Headers["Origin"]);
                    ctx.Response.Headers["Access-Control-Allow-Origin"] = origins;

                    if (ctx.Request.Method == "OPTIONS")
                    {
                        // Handle CORS preflight request
                        ctx.Response.Headers["Access-Control-Allow-Methods"] =
                            "GET, POST, PUT, PATCH, DELETE, OPTIONS";

                        if (ctx.Request.Headers.Keys.Contains("Access-Control-Request-Headers"))
                        {
                            var allowedHeaders = "" + string.Join(
                                ", ", ctx.Request.Headers["Access-Control-Request-Headers"]);
                            ctx.Response.Headers["Access-Control-Allow-Headers"] = allowedHeaders;
                        }
                    }
                }
            });
        }        

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            Config.LuceneNullToken = ConfigurationManager.AppSettings["LuceneNullToken"] ?? Config.LuceneNullToken;

            // There is only one instance of the Database object in the application.
            // It is created here, and registered with the IOC container so that 
            // it can be auto-injected into DbService instances.            
            var db = new Database(Config.DbPath);
            container.Register<Database>(db);                       
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides(
                    c =>
                    {
                        c.ResponseProcessors.Clear();
                        c.ResponseProcessors.Add(typeof(JsonProcessor));
                    }
                );
            }
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            nancyConventions.StaticContentsConventions.AddDirectory(@"/api-spec");
        }
    }
}
