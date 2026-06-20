using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NCalc.BuildTools
{
    /// <summary>
    /// Hosts build-time assembly patching that preserves legacy public surface
    /// after third-party assemblies are merged into the NCalc package.
    /// </summary>
    internal static class Program
    {
        private const string RestoreNet35AntlrFileStreamCommand = "restore-net35-antlr-filestream";
        private const string AntlrFileStreamTypeName = "Antlr.Runtime.ANTLRFileStream";

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || args[0] != RestoreNet35AntlrFileStreamCommand)
                {
                    WriteUsage();
                    return 2;
                }

                string assemblyPath = args[1];
                string strongNameKeyPath = args.Length >= 3 ? args[2] : string.Empty;

                RestoreNet35AntlrFileStreamCompatibility(assemblyPath, strongNameKeyPath);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        private static void RestoreNet35AntlrFileStreamCompatibility(string assemblyPath, string strongNameKeyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Assembly to patch was not found.", assemblyPath);
            }

            string symbolsPath = Path.ChangeExtension(assemblyPath, ".pdb");
            bool readSymbols = File.Exists(symbolsPath);
            ReaderParameters readerParameters = new ReaderParameters
            {
                ReadSymbols = readSymbols,
                ReadingMode = ReadingMode.Immediate
            };

            string temporaryPath;
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters))
            {
                ModuleDefinition module = assembly.MainModule;
                TypeDefinition antlrFileStream = module.Types.FirstOrDefault(type => type.FullName == AntlrFileStreamTypeName);

                if (antlrFileStream == null)
                {
                    throw new InvalidOperationException("Could not find " + AntlrFileStreamTypeName + " in " + assemblyPath + ".");
                }

                bool changed = false;
                changed |= EnsureProtectedDefaultConstructor(module, antlrFileStream);

                if (!changed)
                {
                    Console.WriteLine("ANTLRFileStream legacy members already present.");
                    return;
                }

                temporaryPath = WriteAssemblyToTemporaryFile(assembly, assemblyPath, strongNameKeyPath, readSymbols);
            }

            ReplaceAssemblyWithTemporaryOutput(assemblyPath, temporaryPath, readSymbols);
            Console.WriteLine("Restored net35 ANTLRFileStream legacy members.");
        }

        private static bool EnsureProtectedDefaultConstructor(ModuleDefinition module, TypeDefinition antlrFileStream)
        {
            bool hasDefaultConstructor = antlrFileStream.Methods.Any(method => method.IsConstructor && !method.HasParameters);
            if (hasDefaultConstructor)
            {
                return false;
            }

            MethodReference baseConstructor = ImportDefaultBaseConstructor(module, antlrFileStream);
            MethodDefinition constructor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);

            ILProcessor ilProcessor = constructor.Body.GetILProcessor();
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Call, baseConstructor));
            ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

            antlrFileStream.Methods.Add(constructor);
            return true;
        }

        private static MethodReference ImportDefaultBaseConstructor(ModuleDefinition module, TypeDefinition typeDefinition)
        {
            TypeDefinition baseType = typeDefinition.BaseType.Resolve();
            MethodDefinition baseConstructor = baseType.Methods.FirstOrDefault(method => method.IsConstructor && !method.HasParameters);

            if (baseConstructor == null)
            {
                throw new InvalidOperationException(typeDefinition.BaseType.FullName + " does not expose a default constructor.");
            }

            return module.ImportReference(baseConstructor);
        }

        private static string WriteAssemblyToTemporaryFile(
            AssemblyDefinition assembly,
            string assemblyPath,
            string strongNameKeyPath,
            bool writeSymbols)
        {
            string temporaryPath = assemblyPath + ".patched";
            WriterParameters writerParameters = new WriterParameters { WriteSymbols = writeSymbols };

            if (!string.IsNullOrWhiteSpace(strongNameKeyPath))
            {
                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKeyPath);
            }

            assembly.Write(temporaryPath, writerParameters);
            return temporaryPath;
        }

        private static void ReplaceAssemblyWithTemporaryOutput(string assemblyPath, string temporaryPath, bool writeSymbols)
        {
            File.Copy(temporaryPath, assemblyPath, true);
            File.Delete(temporaryPath);

            if (!writeSymbols)
            {
                return;
            }

            string originalSymbolsPath = Path.ChangeExtension(assemblyPath, ".pdb");
            string assemblyNameSidecarSymbolsPath = assemblyPath + ".pdb";
            string temporarySymbolsPath = temporaryPath + ".pdb";

            if (File.Exists(assemblyNameSidecarSymbolsPath))
            {
                File.Copy(assemblyNameSidecarSymbolsPath, originalSymbolsPath, true);
                File.Delete(assemblyNameSidecarSymbolsPath);
            }

            if (File.Exists(temporarySymbolsPath))
            {
                File.Copy(temporarySymbolsPath, originalSymbolsPath, true);
                File.Delete(temporarySymbolsPath);
            }
        }

        private static void WriteUsage()
        {
            Console.Error.WriteLine("Usage: NCalc.BuildTools restore-net35-antlr-filestream <assembly-path> [strong-name-key-path]");
        }
    }
}
