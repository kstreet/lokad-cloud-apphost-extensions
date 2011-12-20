#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;

namespace App1
{
    public class EntryPoint : IApplicationEntryPoint
    {
        private readonly TextWriter _log;

        public EntryPoint()
        {
            _log = File.AppendText("app1_log.txt");
        }

        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            _log.Write("[App1] {0} {1}: Started {2}", environment.CurrentDeploymentName, environment.CellName, DateTime.Now);

            while (!cancellationToken.IsCancellationRequested)
            {
                _log.Write("[App1] {0} {1}: Timestamp {2}", environment.CurrentDeploymentName, environment.CellName, DateTime.Now);

                cancellationToken.WaitHandle.WaitOne(5000);
            }

            _log.Write("[App1] {0} {1}: Stopped {2}", environment.CurrentDeploymentName, environment.CellName, DateTime.Now);
        }

        public void ApplyChangedSettings(XElement settings)
        {
            _log.Write("[App1] SettingsChanged: {0}", settings);
        }
    }
}
