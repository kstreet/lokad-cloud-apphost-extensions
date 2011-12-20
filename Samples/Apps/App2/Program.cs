#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;

namespace App2
{
    public class Program : IApplicationEntryPoint
    {
        static void Main(string[] args)
        {
            var program = new Program();

            var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    // NOTE: This cancelling logic does not work in the VS2010 debugger with .Net 4, but does fine otherwise
                    tokenSource.Cancel();
                    eventArgs.Cancel = true;
                };

            program.Run(tokenSource.Token, Console.WriteLine);
        }

        void IApplicationEntryPoint.Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            var log = File.AppendText("app2_log.txt");
            Run(cancellationToken, logtext => log.Write("[App1] {0} {1}: {2}", environment.CurrentDeploymentName, environment.CellName, logtext));
        }

        void IApplicationEntryPoint.ApplyChangedSettings(XElement settings)
        {
        }

        public void Run(CancellationToken cancellationToken, Action<string> log)
        {
            log(string.Format("Started {0}", DateTime.Now));

            while (!cancellationToken.IsCancellationRequested)
            {
                log(string.Format("Timestamp {0}", DateTime.Now));

                cancellationToken.WaitHandle.WaitOne(5000);
            }

            log(string.Format("Stopped {0}", DateTime.Now));
        }
    }
}
