using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Ionic.Zip;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureCloud.Worker
{
    [Serializable]
    public class DeploymentReader : IDeploymentReader
    {
        public string ContainerName = "lokad-cloud-assemblies";
        public string PackageBlobName = "default";
        public string ConfigBlobName = "config";
        public string DefaultEntryPoint = "Lokad.Cloud.Autofac.ApplicationEntryPoint, Lokad.Cloud.Autofac";

        public String ConnectionString;

        public DeploymentReader(string connectionString)
        {
            ConnectionString = connectionString;
            _storage = CloudStorageAccount.Parse(connectionString);
        }

        [NonSerialized]
        private CloudStorageAccount _storage;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _storage = CloudStorageAccount.Parse(ConnectionString);
        }

        string GetETagOrNull(string container, string file)
        {
            var blob = _storage.CreateCloudBlobClient().GetBlobDirectoryReference(container).GetBlobReference(file);
            try
            {
                blob.FetchAttributes();
                return blob.Attributes.Properties.ETag;
            }
            catch(StorageClientException ex)
            {
                return null;
            }
        }

        string GetTextOrNull(string container, string file, out string etag)
        {
            var blob = _storage.CreateCloudBlobClient().GetBlobDirectoryReference(container).GetBlobReference(file);
            etag = null;
            try
            {
                blob.FetchAttributes();
                etag = blob.Attributes.Properties.ETag;
                return blob.DownloadText(new BlobRequestOptions()
                    {
                        AccessCondition = AccessCondition.IfMatch(etag)
                    });
            }
            catch (StorageClientException ex)
            {
                etag = null;
                return null;
            }
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            newETag = GetETagOrNull(ContainerName, PackageBlobName) + "|" + GetETagOrNull(ContainerName, ConfigBlobName);
            if (newETag.StartsWith("|"))
                newETag = null;

            if (newETag == null || knownETag != null && knownETag == newETag)
            {
                return null;
            }

            return new SolutionHead(newETag);
        }

        public SolutionDefinition GetSolution(SolutionHead deployment)
        {
            var settings = new XElement("Settings",
                new XElement("DataConnectionString", ConnectionString));

            string entryPointTypeName = null;
            string configEtag;
            var appConfig = GetTextOrNull(ContainerName, ConfigBlobName, out configEtag);
            if (appConfig != null && deployment.SolutionId.EndsWith(configEtag))
            {
                // add raw config to settings (Base64)
                settings.Add(new XElement("RawConfig", appConfig));

                // directly insert config xml root as element, if possible
                try
                {
                    using (var configStream = new StreamReader(appConfig))
                    {
                        var configDoc = XDocument.Load(configStream);
                        if (configDoc != null && configDoc.Root != null)
                        {
                            settings.Add(configDoc.Root);

                            // if root contains "EntryPoint" element with "typeName" attribute, use it as entry point
                            var entryPointXml = configDoc.Root.Element("EntryPoint");
                            XAttribute typeNameXml;
                            if (entryPointXml != null && (typeNameXml = entryPointXml.Attribute("typeName")) != null && !String.IsNullOrWhiteSpace(typeNameXml.Value))
                            {
                                entryPointTypeName = typeNameXml.Value.Trim();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // don't care, unfortunately there's no TryLoad
                }
            }

            string headEtag;
            var headInfo = GetTextOrNull(ContainerName, PackageBlobName, out headEtag);
            if (headInfo != null && deployment.SolutionId.StartsWith(headEtag))
            {
                return new SolutionDefinition("Solution", new[]
                {
                    new CellDefinition("Cell",
                        new AssembliesHead(headInfo),
                        entryPointTypeName ?? DefaultEntryPoint,
                        settings.ToString())
                });
            }
            return null;
        }

        public IEnumerable<AssemblyData> GetAssembliesAndSymbols(AssembliesHead assemblies)
        {

            var blob = _storage.CreateCloudBlobClient()
                .GetBlobDirectoryReference(ContainerName)
                .GetBlobReference(assemblies.AssembliesId);

            using (var zipStream = blob.OpenRead())
            using (var zip = ZipFile.Read(zipStream))
            {
                foreach (var entry in zip)
                {
                    if (entry.IsDirectory || entry.IsText || entry.UncompressedSize == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(entry.FileName);
                    if (extension != ".dll" && extension != ".pdb")
                    {
                        continue;
                    }

                    using (var stream = new MemoryStream())
                    {
                        entry.Extract(stream);
                        yield return new AssemblyData(Path.GetFileName(entry.FileName), stream.ToArray());
                    }
                }
            }
        }

        public T GetItem<T>(string itemName) where T : class
        {
            var blob = _storage.CreateCloudBlobClient().GetBlobDirectoryReference(ContainerName).GetBlobReference(itemName);

            try
            {
                if (typeof(T) == typeof(string))
                    return blob.DownloadText() as T;
                if (typeof(T) == typeof(byte[]))
                    return blob.DownloadByteArray() as T;
                return default(T);

            }
            catch(StorageClientException)
            {
                return default(T);
            }
        }
    }
}