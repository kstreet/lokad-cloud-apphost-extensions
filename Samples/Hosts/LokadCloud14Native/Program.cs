#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reactive.Linq;
using System.Threading;
using Lokad.Cloud.AppHost;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using Lokad.Cloud.AppHost.Framework.Instrumentation.Events;
using LokadCloud14.NativeDeployments;

namespace LokadCloud14.NativeHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instrumentation & Logging
            var observer = new HostObserverSubject();
            observer.OfType<HostStartedEvent>().Subscribe(e => Console.WriteLine("AppHost started on {0}.", e.Host.WorkerName));
            observer.OfType<HostStoppedEvent>().Subscribe(e => Console.WriteLine("AppHost stopped on {0}.", e.Host.WorkerName));
            observer.OfType<NewDeploymentDetectedEvent>().Subscribe(e => Console.WriteLine("New deployment {0} detected for solution {1} on {2}.", e.Deployment.SolutionId, e.Solution.SolutionName, e.Host.WorkerName));
            observer.OfType<NewUnrelatedSolutionDetectedEvent>().Subscribe(e => Console.WriteLine("New unrelated solution {0} detected on {1}.", e.Solution.SolutionName, e.Host.WorkerName));
            observer.OfType<CellStartedEvent>().Subscribe(e => Console.WriteLine("Cell {0} of solution {1} started on {2}.", e.Cell.CellName, e.Cell.SolutionName, e.Cell.Host.WorkerName));
            observer.OfType<CellStoppedEvent>().Subscribe(e => Console.WriteLine("Cell {0} of solution {1} stopped on {2}.", e.Cell.CellName, e.Cell.SolutionName, e.Cell.Host.WorkerName));
            observer.OfType<CellExceptionRestartedEvent>().Subscribe(e => Console.WriteLine("Cell {0} of solution {1} exception: {2}", e.Cell.CellName, e.Cell.SolutionName, e.Exception));
            observer.OfType<CellFatalErrorRestartedEvent>().Subscribe(e => Console.WriteLine("Cell {0} of solution {1} fatal error: {2}", e.Cell.CellName, e.Cell.SolutionName, e.Exception));

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
