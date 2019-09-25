using System.Diagnostics;
using System.IO;

namespace DeepSwarmClient
{
    partial class Engine
    {
        void UpdateScriptText(string relativePath, string scriptText)
        {
            File.WriteAllText(Path.Combine(ScriptsPath, relativePath), scriptText);
            Scripts[relativePath] = scriptText;

            foreach (var (entityId, scriptPath) in EntityScriptPaths)
            {
                if (scriptPath == relativePath) SetupLuaForEntity(entityId, scriptText);
            }
        }

        void SetupLuaForEntity(int entityId, string scriptText)
        {
            if (_luasByEntityId.Remove(entityId, out var oldLua)) oldLua.Dispose();

            if (scriptText != null)
            {
                var lua = new KeraLua.Lua(openLibs: true);
                _luasByEntityId.Add(entityId, lua);

                if (lua.DoString(scriptText))
                {
                    // TODO: Display error in UI / on entity as an icon
                    var error = lua.ToString(-1);
                    Trace.WriteLine("Error: " + error);
                }
            }
        }
    }
}
