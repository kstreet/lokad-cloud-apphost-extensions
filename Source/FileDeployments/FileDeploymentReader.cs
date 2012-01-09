#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;

namespace Lokad.Cloud.AppHost.Extensions.FileDeployments
{
    [Serializable]
    public class FileDeploymentReader : IDeploymentReader
    {
        const string DeploymentHeadFileName = "currentdeployment.txt";
        const string EntryPointFileName = "entrypoint.txt";
        const string SettingsFileName = "settings.xml";
        readonly string _basePath;

        public FileDeploymentReader(string basePath)
        {
            _basePath = basePath;
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            var file = new FileInfo(Path.Combine(_basePath, DeploymentHeadFileName));
            if (!file.Exists)
            {
                newETag = null;
                return null;
            }

            newETag = file.LastWriteTimeUtc.Ticks.ToString();
            if (knownETag != null && knownETag == newETag)
            {
                return null;
            }

            var deploymentId = File.ReadAllText(file.FullName).Trim();
            return new SolutionHead(deploymentId);
        }

        public SolutionDefinition GetSolution(SolutionHead deployment)
        {
            var deploymentDirectory = new DirectoryInfo(Path.Combine(_basePath, deployment.SolutionId));
            if (!deploymentDirectory.Exists)
            {
                return null;
            }

            var cells = new List<CellDefinition>();
            foreach (var cellDirectory in deploymentDirectory.EnumerateDirectories())
            {
                var entryPointFile = new FileInfo(Path.Combine(cellDirectory.FullName, EntryPointFileName));
                if (!entryPointFile.Exists)
                {
                    // skip invalid cell
                    // TODO: notify?
                    continue;
                }

                var settingsFile = new FileInfo(Path.Combine(cellDirectory.FullName, SettingsFileName));
                cells.Add(new CellDefinition(
                    cellDirectory.Name,
                    new AssembliesHead(string.Format("{0}{1}{2}", deployment.SolutionId, Path.DirectorySeparatorChar, cellDirectory.Name)),
                    File.ReadAllText(entryPointFile.FullName).Trim(),
                    settingsFile.Exists ? File.ReadAllText(settingsFile.FullName) : null));
            }

            return new SolutionDefinition("Solution", cells.ToArray());
        }

        public IEnumerable<AssemblyData> GetAssembliesAndSymbols(AssembliesHead assemblies)
        {
            var cellDirectory = new DirectoryInfo(Path.Combine(_basePath, assemblies.AssembliesId));
            if (!cellDirectory.Exists)
            {
                return new AssemblyData[0];
            }

            return cellDirectory.EnumerateFiles("*.dll")
                .Concat(cellDirectory.EnumerateFiles("*.exe"))
                .Concat(cellDirectory.EnumerateFiles("*.pdb"))
                .Select(f => new AssemblyData(f.Name, File.ReadAllBytes(f.FullName)));
        }

        public T GetItem<T>(string itemName) where T : class
        {
            var file = new FileInfo(Path.Combine(_basePath, itemName));
            if (!file.Exists)
            {
                return default(T);
            }

            if (typeof(T).IsAssignableFrom(typeof(XElement)))
            {
                return XDocument.Load(file.FullName).Root as T;
            }

            if (typeof(T).IsAssignableFrom(typeof(byte[])))
            {
                return File.ReadAllBytes(file.FullName) as T;
            }

            throw new NotSupportedException();
        }
    }
}
