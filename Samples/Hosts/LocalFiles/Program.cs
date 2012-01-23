﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Threading;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.AppHost.Extensions.FileDeployments;
using Lokad.Cloud.AppHost.Framework.Instrumentation;

namespace LocalFiles
{
    class Program
    {
        static void Main()
        {
            // Instrumentation & Logging
            var observer = new HostObserverSubject();
            observer.Subscribe(e => Console.WriteLine(e.Describe()));

            // Deployments
            var deploymentPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), @"..\..\Deployment");
            var deploymentReader = new FileDeploymentReader(deploymentPath);

            // Host
            var context = new HostContext(observer, deploymentReader);
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
