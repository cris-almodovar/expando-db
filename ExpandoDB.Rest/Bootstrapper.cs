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

namespace ExpandoDB.Rest
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(Bootstrapper).Name);              

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            StaticConfiguration.DisableErrorTraces = false;

            AppDomain.CurrentDomain.UnhandledException += (sender, ex) =>
            {
                _log.Error(ex);
            };

            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                if (ctx.Request.Headers.Keys.Contains("Origin"))
                {
                    var origins = "" + string.Join(" ", ctx.Request.Headers["Origin"]);
                    ctx.Response.Headers["Access-Control-Allow-Origin"] = origins;

                    if (ctx.Request.Method == "OPTIONS")
                    {
                        // handle CORS preflight request
                        ctx.Response.Headers["Access-Control-Allow-Methods"] =
                            "GET, POST, PUT, DELETE, OPTIONS";

                        if (ctx.Request.Headers.Keys.Contains("Access-Control-Request-Headers"))
                        {
                            var allowedHeaders = "" + string.Join(
                                ", ", ctx.Request.Headers["Access-Control-Request-Headers"]);
                            ctx.Response.Headers["Access-Control-Allow-Headers"] = allowedHeaders;
                        }
                    }
                }
            });

            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
                {
                    _log.Error(ex);
                    var dto = new ErrorResponseDto { timestamp = DateTime.UtcNow, message = ex.Message, statusCode = HttpStatusCode.InternalServerError };
                    var response = new JsonResponse<ErrorResponseDto>(dto, new DefaultJsonSerializer())
                    {
                        StatusCode = HttpStatusCode.InternalServerError
                    };

                    return response;
                }
            );       

            JsonSettings.RetainCasing = true;
            JsonSettings.ISO8601DateFormat = true;            
        }        

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            
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
    }
}
