#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using LokadCloud14.NativeDeployments;

namespace LokadCloud14.NativeHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instrumentation & Logging
            var observer = new HostObserverSubject();
            observer.Subscribe(e => Console.WriteLine(e.Describe()));

            // Deployments
            var deploymentReader = new DeploymentReader("UseDevelopmentStorage=true");

            // Host
            var context = new HostContext(deploymentReader, observer);
            var host = new Host(context);

            // START
            var cts = new CancellationTokenSource();
            host.Run(cts.Token);

            Console.ReadKey();

            // STOP
            cts.Cancel();

            Console.ReadKey();
        }
    }
}
