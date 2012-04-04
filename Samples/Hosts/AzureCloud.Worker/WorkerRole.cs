using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using LokadCloud14.NativeDeployments;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureCloud.Worker
{
    public class WorkerRole : RoleEntryPoint, IObserver<IHostEvent>
    {
        public HostContext Context;
        public Host Host;
        readonly CancellationTokenSource _source = new CancellationTokenSource();

        public override void Run()
        {
            Trace.WriteLine("Starting Host", "Information");
            using(var task =  Host.Run(_source.Token))
            {
                _source.Token.WaitHandle.WaitOne();
                // let task some time for gracefull termination
                Trace.WriteLine("Shutting down", "Information");
                if (!task.Wait(TimeSpan.FromSeconds(30)))
                {
                    Trace.WriteLine("Terminating forcefully", "Error");
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += RoleEnvironmentChanging; 

            var observer = new HostObserverSubject(new []{this});
            
            var conn = AzureSettingsProvider.GetStringOrThrow("DeploymentAccount");
            var reader = new DeploymentReader(conn)
                {
                    ContainerName = AzureSettingsProvider.GetStringOrNull("DeploymentContainer") ?? "lokad-deployment",
                    ConfigBlobName = AzureSettingsProvider.GetStringOrNull("ConfigName") ?? "config.txt",
                    PackageBlobName = AzureSettingsProvider.GetStringOrNull("PackageFile") ?? "package.txt"
                };
            
            Context = new HostContext(reader, observer);
            Host = new Host(Context);

            return base.OnStart();
        }

        void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }

        public void OnNext(IHostEvent value)
        {
            Trace.WriteLine(value.Describe());
        }

        public void OnError(Exception error)
        {
            Trace.WriteLine(error);
        }

        public void OnCompleted()
        {
            
        }
    }
}
