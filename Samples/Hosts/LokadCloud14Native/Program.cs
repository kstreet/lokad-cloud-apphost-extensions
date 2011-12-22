﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Events;
using LokadCloud14.NativeDeployments;

namespace LokadCloud14.NativeHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instrumentation & Logging
            var observer = new HostObserverSubject();
            observer.OfType<HostStartedEvent>().Subscribe(e => Console.WriteLine("AppHost started."));
            observer.OfType<HostStoppedEvent>().Subscribe(e => Console.WriteLine("AppHost stopped."));
            observer.OfType<CellStartedEvent>().Subscribe(e => Console.WriteLine("Cell {0} started.", e.CellName));
            observer.OfType<CellStoppedEvent>().Subscribe(e => Console.WriteLine("Cell {0} stopped.", e.CellName));
            observer.OfType<CellExceptionRestartedEvent>().Subscribe(e => Console.WriteLine("Cell {0} exception: {1}", e.CellName, e.Exception));
            observer.OfType<CellFatalErrorRestartedEvent>().Subscribe(e => Console.WriteLine("Cell {0} fatal error: {1}", e.CellName, e.Exception));

            // Deployments
            var deploymentReader = new DeploymentReader("UseDevelopmentStorage=true");

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
