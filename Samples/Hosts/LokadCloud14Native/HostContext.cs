#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;

namespace LokadCloud14.NativeHost
{
    public class HostContext : IHostContext
    {
        public HostContext(IHostObserver hostObserver, IDeploymentReader deploymentReader)
        {
            Observer = hostObserver;
            DeploymentReader = deploymentReader;
        }

        public int CurrentWorkerInstanceCount
        {
            get { return 1; }
        }

        public IDeploymentReader DeploymentReader { get; private set; }

        /// <remarks>Can be <c>null</c>.</remarks>
        public IHostObserver Observer { get; private set; }

        public string GetSettingValue(string settingName)
        {
            throw new NotImplementedException();
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            throw new NotImplementedException();
        }

        public string GetLocalResourcePath(string resourceName)
        {
            var path = Path.Combine(Path.GetTempPath(), "LokadAppHost", resourceName);
            Directory.CreateDirectory(path);
            return path;
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            throw new NotImplementedException();
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            throw new NotImplementedException();
        }
    }
}
