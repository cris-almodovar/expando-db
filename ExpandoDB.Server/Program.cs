using Nancy.Hosting.Self;
using System;

namespace ExpandoDB.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var hostConfig = new HostConfiguration
            {
                UrlReservations = new UrlReservations { CreateAutomatically = true }
            };
            var host = new NancyHost(hostConfig, new Uri("http://localhost:8080/"));
            host.Start();

            Console.ReadLine();

            host.Stop();
            host.Dispose();
                    
               
        }
    }
}
