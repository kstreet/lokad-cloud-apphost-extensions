#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.AppHost.Framework.Instrumentation;

namespace LocalFilesSimple
{
    public class HostContext : IHostContext
    {
        private readonly HostLifeIdentity _identity;

        public HostContext(IHostObserver hostObserver, IDeploymentReader deploymentReader)
        {
            Observer = hostObserver;
            DeploymentReader = deploymentReader;

            _identity = new HostLifeIdentity(Environment.MachineName, Guid.NewGuid().ToString("N"));
        }

        public HostLifeIdentity Identity
        {
            get { return _identity; }
        }

        public CellLifeIdentity GetNewCellLifeIdentity(string solutionName, string cellName, SolutionHead deployment)
        {
            return new CellLifeIdentity(_identity, solutionName, cellName, Guid.NewGuid().ToString("N"));
        }

        public string GetSettingValue(CellLifeIdentity cell, string settingName)
        {
            return null;
        }

        public X509Certificate2 GetCertificate(CellLifeIdentity cell, string thumbprint)
        {
            return null;
        }

        public string GetLocalResourcePath(CellLifeIdentity cell, string resourceName)
        {
            var path = Path.Combine(Path.GetTempPath(), _identity.UniqueWorkerInstanceName, cell.UniqueCellInstanceName, resourceName);
            Directory.CreateDirectory(path);
            return path;
        }

        public IPEndPoint GetEndpoint(CellLifeIdentity cell, string endpointName)
        {
            return null;
        }

        public int CurrentWorkerInstanceCount
        {
            get { return 1; }
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
        }

        public IDeploymentReader DeploymentReader { get; private set; }

        /// <remarks>Can be <c>null</c>.</remarks>
        public IHostObserver Observer { get; private set; }
    }
}
