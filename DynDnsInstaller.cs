using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DynDns
{
    [RunInstaller(true)]
    public sealed class DynDnsServiceProcessInstaller : ServiceProcessInstaller
    {
        public DynDnsServiceProcessInstaller()
        {
            this.Account = System.ServiceProcess.ServiceAccount.NetworkService;
            this.Password = null;
            this.Username = null;
        }
    }

    [RunInstaller(true)]
    public sealed class DynDnsServiceInstaller : ServiceInstaller
    {
        public DynDnsServiceInstaller()
        {
            this.Description = "ZoneEdit Dynamic Dns IP Update Client";
            this.DisplayName = "ZoneEdit Dynamic Dns Client";
            this.ServiceName = DynDnsClient.SERVICE_NAME;
            this.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        }

        protected override void OnAfterInstall(System.Collections.IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            
            // TODO: set service automatic restart?

            // start service after install
            using (var sc = new System.ServiceProcess.ServiceController(this.ServiceName))
            {
                sc.Start();
            }
        }
    }
}