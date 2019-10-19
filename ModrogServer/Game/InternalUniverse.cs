using SwarmBasics.Math;
using ModrogCommon.Scripting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModrogServer.Game
{
    sealed class InternalUniverse : ModrogApi.Server.Universe, IDisposable
    {
        internal readonly InternalPlayer[] Players;
        internal readonly List<InternalTileKind> TileKinds = new List<InternalTileKind>();
        internal readonly List<InternalWorld> _worlds = new List<InternalWorld>();

        internal string SpritesheetPath { get; private set; } = "Spritesheet.png";

        ScriptContext _scriptContext;
        ModrogApi.Server.IScenarioScript _script;

        internal int TickIndex { get; private set; } = -1;
        int _nextEntityId = 0;

        internal InternalUniverse(InternalPlayer[] players, string scenarioPath)
        {
            Players = players;

            TileKinds.Add(new InternalTileKind(0, Point.Zero, ModrogApi.Server.TileFlags.Opaque | ModrogApi.Server.TileFlags.Solid)); // Default

            var scenarioScriptsPath = Path.Combine(scenarioPath, "Scripts");
            var scriptFilePaths = Directory.GetFiles(scenarioScriptsPath, "*.cs", SearchOption.AllDirectories);
            var scriptFileContents = new string[scriptFilePaths.Length];
            for (var i = 0; i < scriptFilePaths.Length; i++) scriptFileContents[i] = File.ReadAllText(scriptFilePaths[i]);

            var basicsRef = MetadataReference.CreateFromFile(typeof(SwarmBasics.Math.Point).Assembly.Location);
            var apiRef = MetadataReference.CreateFromFile(typeof(ModrogApi.Server.Universe).Assembly.Location);

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

            var scenarioScriptConstructor = scenarioScriptType.GetConstructor(new Type[] { typeof(ModrogApi.Server.Universe) });
            if (scenarioScriptConstructor == null)
            {
                Console.WriteLine($"Could not find ScenarioScript({typeof(ModrogApi.Server.Universe).FullName}) constructor in scripts");
                throw new NotImplementedException("TODO: Handle error");
            }

            try
            {
                _script = (ModrogApi.Server.IScenarioScript)scenarioScriptConstructor.Invoke(new object[] { this });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.InnerException);
                throw new NotImplementedException("TODO: Handle error");
            }
        }

        public void Dispose()
        {
            _script = null;
            _scriptContext.Dispose();
            _scriptContext = null;
        }

        internal int GetNextEntityId() => _nextEntityId++;

        internal void Tick()
        {
            // Apply planned moves
            foreach (var world in _worlds) world.Tick();

            // TODO: Resolve damage, etc.

            // 
            _script.Tick();
        }

        #region API
        public override void SetSpritesheet(string path) { SpritesheetPath = path; }

        public override ModrogApi.Server.Player[] GetPlayers() => Players;

        public override ModrogApi.Server.EntityKind CreateEntityKind(Point spriteLocation)
        {
            var entityKind = new InternalEntityKind(spriteLocation);
            return entityKind;
        }

        public override ModrogApi.Server.TileKind CreateTileKind(Point spriteLocation, ModrogApi.Server.TileFlags flags)
        {
            var tileKind = new InternalTileKind((short)TileKinds.Count, spriteLocation, flags);
            TileKinds.Add(tileKind);
            return tileKind;
        }

        public override ModrogApi.Server.World CreateWorld(int width, int height)
        {
            var world = new InternalWorld(this, width, height);
            _worlds.Add(world);
            return world;
        }
        #endregion
    }
}
