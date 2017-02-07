using Common.Logging;
using System;
using Topshelf;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Implements the application's Main() method.
    /// </summary>
    class EntryPoint
    {
        static void Main()
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
