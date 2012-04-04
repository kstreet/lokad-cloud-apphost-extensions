using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.AppHost.Framework.Instrumentation;

namespace AzureCloud.Worker
{
    public class HostContext : IHostContext
    {
        private readonly HostLifeIdentity _identity;

        public HostContext(IDeploymentReader deploymentReader, IHostObserver observer)
        {
            Observer = observer;
            DeploymentReader = deploymentReader;

            // TODO: Replace GUID with global blob counter
            _identity = new HostLifeIdentity(Environment.MachineName, Guid.NewGuid().ToString("N"));
        }

        public HostLifeIdentity Identity
        {
            get { return _identity; }
        }

        public CellLifeIdentity GetNewCellLifeIdentity(string solutionName, string cellName, SolutionHead deployment)
        {
            // TODO: Replace GUID with global blob counter
            return new CellLifeIdentity(_identity, solutionName, cellName, Guid.NewGuid().ToString("N"));
        }

        public string GetSettingValue(CellLifeIdentity cell, string settingName)
        {
            string value;
            AzureSettingsProvider.TryGetString(settingName, out value);
            return value;
        }

        public X509Certificate2 GetCertificate(CellLifeIdentity cell, string thumbprint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count != 1)
                {
                    return null;
                }

                return certs[0];
            }
            finally
            {
                store.Close();
            }
        }

        public string GetLocalResourcePath(CellLifeIdentity cell, string resourceName)
        {
            var dir = Path.Combine(Path.GetTempPath(), _identity.UniqueWorkerInstanceName, cell.UniqueCellInstanceName, resourceName);
            Directory.CreateDirectory(dir);
            return dir;
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