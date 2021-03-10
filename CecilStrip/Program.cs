using Ganss.IO;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace DllStrip
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputFolder = Path.Join(Environment.CurrentDirectory, "out");
            var resolve = new List<string>();
            var exclusions = new List<string>();

            var options = new OptionSet
            {
                { "o|output=", "Output folder", o => outputFolder = o },
                { "v", "Increase verbosity", v =>
                    {
                        if (v != null) Logger.Verbosity++;
                    }
                },
                { "r|resolve=", "Additional resolve folders", r => resolve.Add(r) },
                { "e|exclude=", "Excluded files", e => exclusions.Add(e) }
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException ex)
            {
                Logger.Error(ex);
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (IOException ex)
                {
                    Logger.Error(ex);
                    return;
                }
            }

            if (!Path.IsPathRooted(outputFolder))
            {
                outputFolder = Path.Combine(Environment.CurrentDirectory, outputFolder);
            }

            Logger.Trace($"Writing files to '{outputFolder}'", 1);

            var excludedFiles = new HashSet<string>();

            foreach (string exclusion in exclusions)
            {
                foreach (string file in Glob.ExpandNames(exclusion))
                {
                    excludedFiles.Add(file);
                }
            }

            foreach (string extra in extras)
            {
                foreach (string fileName in Glob.ExpandNames(extra))
                {
                    if (excludedFiles.Contains(fileName))
                    {
                        Logger.Trace($"Excluding file '{fileName}'", 2);
                        continue;
                    }

                    try
                    {
                        List<string> paths = new List<string>(resolve);
                        paths.Add(Path.GetDirectoryName(fileName));
                        GenerateStrippedAssembly(fileName, outputFolder, paths);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }
        }

        private static void GenerateStrippedAssembly(string filePath, string outputFolder, IEnumerable<string> additionalResolvePaths)
        {
            Logger.Info($"Processing '{filePath}'...");

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                InMemory = true,
                ReadWrite = false,
                AssemblyResolver = new Resolver(additionalResolvePaths)
            });

            foreach (ModuleDefinition module in assembly.Modules)
            {
                Logger.Trace($"Processing module '{module.Name}'", 1);

                foreach (TypeDefinition type in module.Types)
                {
                    ClearType(type);
                }

                module.Resources.Clear();
            }

            string outputFilePath = Path.Join(outputFolder, Path.GetFileName(filePath));

            Logger.Info($"Saving to '{outputFilePath}'...");

            assembly.Write(outputFilePath);
        }

        private static void ClearType(TypeDefinition type)
        {
            Logger.Trace($"Clearing type '{type.FullName}'", 2);

            foreach (MethodDefinition constructor in type.GetConstructors())
            {
                Logger.Trace($"Clearing constructor '{constructor.Name}'", 3);

                constructor.Body?.Instructions.Clear();
            }

            foreach (MethodDefinition method in type.Methods)
            {
                Logger.Trace($"Clearing method '{method.Name}'", 3);

                method.Body?.Instructions.Clear();
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                Logger.Trace($"Clearing property '{property.Name}'", 3);

                property.GetMethod?.Body?.Instructions.Clear();
                property.SetMethod?.Body?.Instructions.Clear();

                foreach (MethodDefinition method in property.OtherMethods)
                {
                    method.Body?.Instructions.Clear();
                }
            }

            foreach (EventDefinition evt in type.Events)
            {
                Logger.Trace($"Clearing event '{evt.Name}'", 3);

                evt.AddMethod?.Body?.Instructions.Clear();
                evt.RemoveMethod?.Body?.Instructions.Clear();
                evt.InvokeMethod?.Body?.Instructions.Clear();

                foreach (MethodDefinition method in evt.OtherMethods)
                {
                    method.Body?.Instructions.Clear();
                }
            }

            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                ClearType(nestedType);
            }
        }

        private class Resolver : BaseAssemblyResolver
        {
            private DefaultAssemblyResolver defaultResolver;
            private IEnumerable<string> paths;

            public Resolver(IEnumerable<string> paths)
            {
                this.defaultResolver = new DefaultAssemblyResolver();
                this.paths = paths;
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                foreach (string directory in paths)
                {
                    string file = Path.Combine(directory, name.Name + ".dll");

                    if (File.Exists(file))
                    {
                        return AssemblyDefinition.ReadAssembly(file);
                    }
                }
                
                return defaultResolver.Resolve(name);
            }
        }
    }
}
