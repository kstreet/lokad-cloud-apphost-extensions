#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;

namespace LokadCloud14.NativeDeployments
{
    public class EntryPoint : IApplicationEntryPoint
    {
        public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void ApplyChangedSettings(XElement settings)
        {
            throw new NotImplementedException();
        }
    }
}
