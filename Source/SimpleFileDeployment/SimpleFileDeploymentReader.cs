#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Mono.Cecil;

namespace Lokad.Cloud.AppHost.Extensions.SimpleFileDeployment
{
    [Serializable]
    public class SimpleFileDeploymentReader : IDeploymentReader
    {
        private readonly string _basePath;
        private readonly FileSystemWatcher _watcher;
        private long _filesVersion;

        public SimpleFileDeploymentReader(string basePath)
        {
            _basePath = basePath;
            _watcher = new FileSystemWatcher(basePath);
            _watcher.Changed += (sender, args) => Interlocked.Increment(ref _filesVersion);
            _watcher.Created += (sender, args) => Interlocked.Increment(ref _filesVersion);
            _watcher.Renamed += (sender, args) => Interlocked.Increment(ref _filesVersion);
            _watcher.EnableRaisingEvents = true;
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            var version = Interlocked.Read(ref _filesVersion);
            newETag = version.ToString();
            if (newETag == knownETag)
            {
                return null;
            }

            return new SolutionHead(newETag);
        }

        public SolutionDefinition GetSolution(SolutionHead deployment)
        {
            if (!Directory.Exists(_basePath))
            {
                return null;
            }

            var interfaceWeAreLookingFor = typeof (IApplicationEntryPoint).FullName;
            var entryPointType = Directory.EnumerateFiles(_basePath, "*.dll", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(_basePath, "*.exe", SearchOption.AllDirectories))
                .Select(AssemblyDefinition.ReadAssembly)
                .SelectMany(a => a.MainModule.Types)
                .FirstOrDefault(t => t.Interfaces.Any(i => i.FullName == interfaceWeAreLookingFor));

            if (entryPointType == null)
            {
                return null;
            }

            return new SolutionDefinition("Solution", new[]
                {
                    new CellDefinition("Cell",
                        new AssembliesHead(deployment.SolutionId),
                        string.Format("{0}, {1}", entryPointType.FullName, Path.GetFileNameWithoutExtension(entryPointType.Module.Name)),
                        null)
                });
        }

        public IEnumerable<AssemblyData> GetAssembliesAndSymbols(AssembliesHead assemblies)
        {
            if (!Directory.Exists(_basePath))
            {
                return new AssemblyData[0];
            }

            return Directory.EnumerateFiles(_basePath, "*.dll", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(_basePath, "*.exe", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(_basePath, "*.pdb", SearchOption.AllDirectories))
                .Select(path => new AssemblyData(Path.GetFileName(path), File.ReadAllBytes(path)));
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
