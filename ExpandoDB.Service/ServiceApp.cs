using log4net;
using Nancy.Hosting.Self;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace ExpandoDB.Rest
{

    /// <summary>
    /// Wraps ExpandoDB in a Service application.
    /// </summary>
    public class ServiceApp 
    {
        private NancyHost _nancyHost;
        private readonly HostConfiguration _nancyHostConfig;
        private readonly Uri _baseUri;
        private readonly ILog _log = LogManager.GetLogger(typeof(ServiceApp).Name);
        private const string DEFAULT_BASE_URL = @"http://localhost:9000/";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceApp"/> class.
        /// </summary>
        public ServiceApp()
        {
            var appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(appDirectory);

            var baseUriString = ConfigurationManager.AppSettings["RestService.BaseUrl"] ?? DEFAULT_BASE_URL;
            if (!baseUriString.Trim().EndsWith("/", StringComparison.InvariantCulture))
                baseUriString += "/";

            _baseUri = new Uri(baseUriString);
            _nancyHostConfig = new HostConfiguration
            {
                UrlReservations = new UrlReservations { CreateAutomatically = true }
            };            
        }

        /// <summary>
        /// Starts the ExpandoDB server
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            _log.Info("---------------------------------------------");
            _log.Info("Starting ExpandoDB service ...");            

            _nancyHost = new NancyHost(_nancyHostConfig, _baseUri);
            _nancyHost.Start();

            _log.Info("ExpandoDB service started successfully.");
            _log.Info("---------------------------------------------");

            return true;
        }

        /// <summary>
        /// Stops the ExpandoDB server
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            _nancyHost.Dispose();
            _nancyHost.Stop();

            _log.Info("ExpandoDB service stopped successfully.");

            return true;
        }        
    }
}
