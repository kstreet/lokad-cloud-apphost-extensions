#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Ionic.Zip;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.Storage;

namespace LokadCloud14.NativeDeployments
{
    [Serializable]
    public class DeploymentReader : IDeploymentReader
    {
        public string ContainerName = "lokad-cloud-assemblies";
        public string PackageBlobName = "default";
        public string ConfigBlobName = "config";

        private readonly string _connectionString;

        public DeploymentReader(string connectionString)
        {
            _connectionString = connectionString;
            _storage = CloudStorage.ForAzureConnectionString(connectionString).BuildStorageProviders();
        }

        [NonSerialized]
        private CloudStorageProviders _storage;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _storage = CloudStorage.ForAzureConnectionString(_connectionString).BuildStorageProviders();
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            newETag = CombineEtags(
                _storage.BlobStorage.GetBlobEtag(ContainerName, PackageBlobName),
                _storage.BlobStorage.GetBlobEtag(ContainerName, ConfigBlobName));

            if (newETag == null || knownETag != null && knownETag == newETag)
            {
                return null;
            }

            return new SolutionHead(newETag);
        }

        public SolutionDefinition GetSolution(SolutionHead deployment)
        {
            var settings = new XElement("Settings",
                    new XElement("DataConnectionString", _connectionString));

            string entryPointTypeName = null;
            string configEtag;
            var appConfig = _storage.BlobStorage.GetBlob<byte[]>(ContainerName, ConfigBlobName, out configEtag);
            if (appConfig.HasValue && configEtag == ConfigEtagOfCombinedEtag(deployment.SolutionId))
            {
                // add raw config to settings (Base64)
                settings.Add(new XElement("RawConfig", Convert.ToBase64String(appConfig.Value)));

                // directly insert config xml root as element, if possible
                try
                {
                    using (var configStream = new MemoryStream(appConfig.Value))
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

            return new SolutionDefinition("Solution", new[]
                {
                    new CellDefinition("Cell",
                        new AssembliesHead(PackageEtagOfCombinedEtag(deployment.SolutionId)),
                        entryPointTypeName ?? "Lokad.Cloud.Autofac.ApplicationEntryPoint, Lokad.Cloud.Autofac",
                        settings.ToString())
                });
        }

        public IEnumerable<AssemblyData> GetAssembliesAndSymbols(AssembliesHead assemblies)
        {
            string packageEtag;
            var packageBlob = _storage.BlobStorage.GetBlob<byte[]>(ContainerName, PackageBlobName, out packageEtag);
            if (!packageBlob.HasValue || packageEtag != assemblies.AssembliesId)
            {
                yield break;
            }

            using (var zipStream = new MemoryStream(packageBlob.Value))
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
            return _storage.BlobStorage.GetBlob<T>(ContainerName, itemName).GetValue(default(T));
        }

        static string CombineEtags(string packageEtag, string configEtag)
        {
            if (packageEtag == null)
            {
                return null;
            }

            var prefix = packageEtag.Length.ToString("0000");
            return configEtag == null
                ? string.Concat(prefix, packageEtag)
                : string.Concat(prefix, packageEtag, configEtag);
        }

        static string PackageEtagOfCombinedEtag(string combinedEtag)
        {
            if (combinedEtag == null || combinedEtag.Length <= 4)
            {
                return null;
            }

            var packageEtag = combinedEtag.Substring(4, Int32.Parse(combinedEtag.Substring(0, 4)));
            return string.IsNullOrEmpty(packageEtag) ? null : packageEtag;
        }

        static string ConfigEtagOfCombinedEtag(string combinedEtag)
        {
            if (combinedEtag == null || combinedEtag.Length <= 5)
            {
                return null;
            }

            var configEtag = combinedEtag.Substring(4 + Int32.Parse(combinedEtag.Substring(0, 4)));
            return string.IsNullOrEmpty(configEtag) ? null : configEtag;
        }
    }
}
