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
        private readonly ILog _log = LogManager.GetLogger(nameof(ServiceApp));
        private const string DEFAULT_BASE_URL = @"http://localhost:9000/";

        /// <summary>
        /// Initializes the <see cref="ServiceApp"/> class.
        /// </summary>
        static ServiceApp()
        {
            var appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(appDirectory);

            var logFolder = ConfigurationManager.AppSettings["App.LogPath"] ?? "log";
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            var logFile = Path.Combine(logFolder, "ExpandoDB.log");
            GlobalContext.Properties["App.LogFilename"] = logFile;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceApp"/> class.
        /// </summary>
        public ServiceApp()
        { 
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
            _nancyHost = new NancyHost(_nancyHostConfig, _baseUri);
            _nancyHost.Start();            
            
            _log.Info($"REST Web Service endpoint: {_baseUri}");

            var logFile = GlobalContext.Properties["App.LogFilename"];
            _log.Info($"Log file: {logFile}");

            _log.Info("---------------------------------------------");
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

            _log.Info("---------------------------------------------");
            _log.Info("ExpandoDB service stopped successfully.");
            _log.Info("---------------------------------------------");

            return true;
        }        
    }
}
