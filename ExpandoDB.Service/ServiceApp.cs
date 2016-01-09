using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
using Topshelf;
using Topshelf.ServiceConfigurators;
using log4net;

namespace ExpandoDB.Rest
{

    /// <summary>
    /// Represents the ExpandoDB Server application.
    /// </summary>
    public class ServiceApp 
    {
        private NancyHost _nancyHost;
        private readonly HostConfiguration _nancyHostConfig;
        private readonly Uri _baseUri;
        private readonly ILog _log = LogManager.GetLogger(typeof(ServiceApp).Name);        

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceApp"/> class.
        /// </summary>
        public ServiceApp()
        {
            var appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(appDirectory);

            var baseUriString = ConfigurationManager.AppSettings["BaseUrl"] ?? "http://localhost:9000/";
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
