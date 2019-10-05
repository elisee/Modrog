using DeepSwarmApi.Server;
using DeepSwarmCommon.Scripting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DeepSwarmServer.Game
{
    sealed class Universe : IDisposable
    {
        public readonly Dictionary<int, Player> PlayersById = new Dictionary<int, Player>();

        ScriptContext _scriptContext;
        readonly IScenarioScript _script;

        public int TickIndex { get; private set; } = -1;

        public Universe(Player[] players, string scenarioPath)
        {
            foreach (var player in players) PlayersById[player.Id] = player;

            var scenarioScriptsPath = Path.Combine(scenarioPath, "Scripts");
            var scriptFilePaths = Directory.GetFiles(scenarioScriptsPath, "*.cs", SearchOption.AllDirectories);
            var scriptFileContents = new string[scriptFilePaths.Length];
            for (var i = 0; i < scriptFilePaths.Length; i++) scriptFileContents[i] = File.ReadAllText(scriptFilePaths[i]);

            var basicsRef = MetadataReference.CreateFromFile(typeof(DeepSwarmBasics.Math.Point).Assembly.Location);
            var apiRef = MetadataReference.CreateFromFile(typeof(ServerApi).Assembly.Location);

            if (!ScriptContext.TryBuild("ScenarioScript", scriptFileContents, new[] { basicsRef, apiRef }, out _scriptContext, out var emitResult))
            {
                Console.WriteLine("Failed to build scripts for scenario:");
                foreach (var diagnostic in emitResult.Diagnostics) Console.WriteLine(diagnostic);
                throw new NotImplementedException("TODO: Handle error");
            }

            var scenarioScriptType = _scriptContext.Assembly.GetType("ScenarioScript");
            if (scenarioScriptType == null)
            {
                Console.WriteLine("Could not find ScenarioScript class in scripts");
                throw new NotImplementedException("TODO: Handle error");
            }

            var scenarioScriptConstructor = scenarioScriptType.GetConstructor(new Type[] { typeof(ServerApi) });
            if (scenarioScriptConstructor == null)
            {
                Console.WriteLine($"Could not find ScenarioScript({typeof(ServerApi).FullName}) constructor in scripts");
                throw new NotImplementedException("TODO: Handle error");
            }

            var apiConstructor = typeof(ServerApi).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(DeepSwarmApi.Server.Player[]) }, null);
            var apiPlayers = new DeepSwarmApi.Server.Player[1] { new DeepSwarmApi.Server.Player() };
            var api = apiConstructor.Invoke(new object[] { apiPlayers });

            try
            {
                _script = (IScenarioScript)scenarioScriptConstructor.Invoke(new object[] { api });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.InnerException);
                throw new NotImplementedException("TODO: Handle error");
            }

        }

        public void Dispose()
        {
            _scriptContext.Dispose();
        }

        public void Tick()
        {
            // TODO: Apply planned moves
            // TODO: Tick entities

            _script.Tick();
        }
    }
}
