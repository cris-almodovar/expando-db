using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace ExpandoDB.Service
{
    /// <summary>
    /// Contains the application's Main() method.
    /// </summary>
    class EntryPoint
    {        
        static void Main(string[] args)
        {
            HostFactory.Run(
                hc =>
                {
                    hc.Service<ServiceApp>(
                        sc =>
                        {
                            sc.ConstructUsing(() => new ServiceApp());
                            sc.WhenStarted(sa => sa.Start());
                            sc.WhenStopped(sa => sa.Stop());
                        }
                    );
                    hc.UseAssemblyInfoForServiceInfo();
                    hc.RunAsLocalSystem();
                    hc.StartAutomatically();
                }
            );
        }
    }
}
