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

        public XElement GetHeadIfModified(string knownETag, out string newETag)
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

            var deploymentName = File.ReadAllText(file.FullName).Trim();
            return new XElement("Head", new XElement("Deployment", new XAttribute("name", deploymentName)));
        }

        public XElement GetDeployment(string deploymentName)
        {
            var deploymentDirectory = new DirectoryInfo(Path.Combine(_basePath, deploymentName));
            if (!deploymentDirectory.Exists)
            {
                return null;
            }

            var cells = new XElement("Cells");
            foreach (var cellDirectory in deploymentDirectory.EnumerateDirectories())
            {
                var entryPointFile = new FileInfo(Path.Combine(cellDirectory.FullName, EntryPointFileName));
                if (!entryPointFile.Exists)
                {
                    // skip invalid cell
                    // TODO: notify?
                    continue;
                }

                var entryPoint = File.ReadAllText(entryPointFile.FullName).Trim();
                var cell = new XElement("Cell",
                    new XAttribute("name", cellDirectory.Name),
                    new XElement("Assemblies", new XAttribute("name", string.Format("{0}{1}{2}", deploymentName, Path.DirectorySeparatorChar, cellDirectory.Name))),
                    new XElement("EntryPoint", new XAttribute("typeName", entryPoint)));

                var settingsFile = new FileInfo(Path.Combine(cellDirectory.FullName, SettingsFileName));
                if (settingsFile.Exists)
                {
                    cell.Add(XDocument.Load(settingsFile.FullName).Root);
                }

                cells.Add(cell);
            }

            return new XElement("Deployment", cells);
        }

        public IEnumerable<Tuple<string, byte[]>> GetAssembliesAndSymbols(string assembliesName)
        {
            var cellDirectory = new DirectoryInfo(Path.Combine(_basePath, assembliesName));
            if (!cellDirectory.Exists)
            {
                return new Tuple<string, byte[]>[0];
            }

            return cellDirectory.EnumerateFiles("*.dll")
                .Union(cellDirectory.EnumerateFiles("*.exe"))
                .Union(cellDirectory.EnumerateFiles("*.pdb"))
                .Select(f => new Tuple<string, byte[]>(f.Name, File.ReadAllBytes(f.FullName)));
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
