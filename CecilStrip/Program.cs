using Ganss.IO;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace DllStrip
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputFolder = Path.Join(Environment.CurrentDirectory, "out");

            var options = new OptionSet
            {
                { "o|output=", "Output folder", o => outputFolder = o },
                { "v", "Increase verbosity", v =>
                    {
                        if (v != null) Logger.Verbosity++;
                    }
                }
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

            foreach (string extra in extras)
            {
                foreach (IFileSystemInfo file in Glob.Expand(extra))
                {
                    GenerateStrippedAssembly(file.FullName, outputFolder);
                }
            }
        }

        private static void GenerateStrippedAssembly(string filePath, string outputFolder)
        {
            Logger.Info($"Processing '{filePath}'...");

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                InMemory = true,
                ReadWrite = false,
                AssemblyResolver = new Resolver(filePath)
            });

            foreach (ModuleDefinition module in assembly.Modules)
            {
                Logger.Trace($"Processing module '{module.Name}'", 1);

                foreach (TypeDefinition type in module.Types)
                {
                    ClearType(type);
                }
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
            private string directory;

            public Resolver(string path)
            {
                this.defaultResolver = new DefaultAssemblyResolver();
                this.directory = Path.GetDirectoryName(path);
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                string file = Path.Combine(directory, name.Name + ".dll");

                if (File.Exists(file))
                {
                    return AssemblyDefinition.ReadAssembly(file);
                }
                else
                {
                    return defaultResolver.Resolve(name);
                }
            }
        }
    }
}
