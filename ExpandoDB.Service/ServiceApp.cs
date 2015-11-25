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

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace ExpandoDB.Service
{

    /// <summary>
    /// Represents the ExpandoDB Server application.
    /// </summary>
    public class ServiceApp 
    {
        private NancyHost _nancyHost;
        private readonly HostConfiguration _nancyHostConfig;
        private readonly Uri _baseUri;        

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
            _nancyHost = new NancyHost(_nancyHostConfig, _baseUri);
            _nancyHost.Start();

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
            return true;
        }        
    }
}
