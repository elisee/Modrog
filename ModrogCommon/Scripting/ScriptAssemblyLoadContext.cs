using System;
using System.Reflection;
using System.Runtime.Loader;

namespace ModrogCommon.Scripting
{
    class ScriptAssemblyLoadContext : AssemblyLoadContext
    {
        public ScriptAssemblyLoadContext() : base(isCollectible: true)
        {
        }

        // Prevent loading any DLLs other that the ones we provide
        protected override Assembly Load(AssemblyName assemblyName) => null;
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) => IntPtr.Zero;
    }
}
