using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.IO;
using System.Reflection;

namespace DeepSwarmCommon.Scripting
{
    // See https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples
    public sealed class ScriptContext : IDisposable
    {
        public readonly Assembly Assembly;
        readonly ScriptAssemblyLoadContext _assemblyLoadContext = new ScriptAssemblyLoadContext();

        ScriptContext(MemoryStream stream)
        {
            Assembly = _assemblyLoadContext.LoadFromStream(stream);
        }

        public void Dispose()
        {
            _assemblyLoadContext.Unload();
        }

        public static bool TryBuild(string assemblyName, string[] fileContents, MetadataReference[] assemblyRefs, out ScriptContext scriptContext, out EmitResult emitResult)
        {
            scriptContext = null;

            var syntaxTrees = new SyntaxTree[fileContents.Length];

            for (var i = 0; i < fileContents.Length; i++)
            {
                syntaxTrees[i] = CSharpSyntaxTree.ParseText(fileContents[i], CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

                // TODO: Analyze the script for dangerous accesses that we might want to prevent?
                // maybe CSharpSyntaxWalker, see https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis
                // var root = syntaxTrees[i].GetCompilationUnitRoot();
            }

            var allAssemblyRefs = new MetadataReference[2 + assemblyRefs.Length];
            var privateCoreLibPath = typeof(object).Assembly.Location;
            allAssemblyRefs[0] = MetadataReference.CreateFromFile(privateCoreLibPath);
            allAssemblyRefs[1] = MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(privateCoreLibPath), "System.Runtime.dll"));
            for (var i = 0; i < assemblyRefs.Length; i++) allAssemblyRefs[2 + i] = assemblyRefs[i];

            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, allAssemblyRefs,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release));

            using (var stream = new MemoryStream())
            {
                emitResult = compilation.Emit(stream);
                if (!emitResult.Success) return false;

                stream.Position = 0;
                scriptContext = new ScriptContext(stream);
            }

            return true;
        }
    }
}
