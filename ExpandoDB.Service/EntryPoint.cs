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
        private static readonly ILog _log = LogManager.GetLogger(nameof(EntryPoint));

        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, ex) => _log.Error("FATAL ERROR", ex.ExceptionObject as Exception);

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
