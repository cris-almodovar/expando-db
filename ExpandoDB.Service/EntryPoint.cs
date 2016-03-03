using Topshelf;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Implements the application's Main() method.
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
