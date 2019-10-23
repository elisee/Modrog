﻿using Microsoft.CodeAnalysis;
using ModrogCommon;
using ModrogCommon.Scripting;
using SwarmBasics.Math;
using SwarmCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ModrogServer.Game
{
    sealed class InternalUniverse : ModrogApi.Server.Universe, IDisposable
    {
        internal readonly InternalPlayer[] Players;
        internal readonly List<InternalTileKind>[] TileKindsPerLayer = new List<InternalTileKind>[(int)ModrogApi.MapLayer.Count];
        internal readonly List<InternalWorld> _worlds = new List<InternalWorld>();

        internal string ScenarioPath { get; private set; }
        internal string SpritesheetPath { get; private set; } = "Spritesheet.png";

        ScriptContext _scriptContext;
        ModrogApi.Server.IScenarioScript _script;

        internal int TickIndex { get; private set; } = -1;
        int _nextEntityId = 0;

        internal InternalUniverse(InternalPlayer[] players, string scenarioPath)
        {
            Players = players;
            ScenarioPath = scenarioPath;

            for (var i = 0; i < TileKindsPerLayer.Length; i++) TileKindsPerLayer[i] = new List<InternalTileKind>();

            var scenarioScriptsPath = Path.Combine(ScenarioPath, "Scripts");
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
        public override void LoadTileSet(string tileSetPath)
        {
            JsonElement tileSetJson;
            try
            {
                tileSetJson = JsonHelper.Parse(File.ReadAllText(Path.Combine(ScenarioPath, tileSetPath)));

                SpritesheetPath = tileSetJson.GetProperty("spritesheet").GetString();

                var tileKindsJson = tileSetJson.GetProperty("tileKinds");

                for (var layer = 0; layer < (int)ModrogApi.MapLayer.Count; layer++)
                {
                    var layerName = Enum.GetName(typeof(ModrogApi.MapLayer), layer);

                    if (tileKindsJson.TryGetProperty(layerName, out var layerJson))
                    {
                        TileKindsPerLayer[layer] = new List<InternalTileKind>();

                        for (var i = 0; i < layerJson.GetArrayLength(); i++)
                        {
                            var tileKindJson = layerJson[i];

                            var name = tileKindJson.GetProperty("name").GetString();

                            var spriteLocationJson = tileKindJson.GetProperty("spriteLocation");
                            var spriteLocation = new Point(
                                spriteLocationJson[0].GetInt32(),
                                spriteLocationJson[1].GetInt32());

                            TileKindsPerLayer[layer].Add(new InternalTileKind((short)i, spriteLocation));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception($"Error while loading tile set from {tileSetPath}: " + exception.Message);
            }
        }

        public override ModrogApi.Server.Map LoadMap(string mapPath)
        {
            var map = new InternalMap();

            try
            {
                var reader = new PacketReader();
                reader.Open(File.ReadAllBytes(Path.Combine(ScenarioPath, mapPath)));

                // TODO: Ignored for now, should be used to offset the tile indices probably?
                var tileSetPath = reader.ReadByteSizeString();

                var chunksCount = reader.ReadInt();

                for (var i = 0; i < chunksCount; i++)
                {
                    var coords = new Point(reader.ReadInt(), reader.ReadInt());
                    var tilesPerLayer = new short[(int)ModrogApi.MapLayer.Count][];

                    for (var j = 0; j < (int)ModrogApi.MapLayer.Count; j++)
                    {
                        tilesPerLayer[j] = MemoryMarshal.Cast<byte, short>(reader.ReadBytes(Protocol.MapChunkSide * Protocol.MapChunkSide * sizeof(short))).ToArray();
                    }

                    var chunk = new Chunk(tilesPerLayer);
                    map.Chunks.Add(coords, chunk);
                }
            }
            catch (Exception exception)
            {
                throw new Exception($"Error while loading map from {mapPath}: " + exception.Message);
            }

            return map;
        }

        public override void SetSpritesheet(string path) { SpritesheetPath = path; }

        public override ModrogApi.Server.Player[] GetPlayers() => Players;

        public override ModrogApi.Server.EntityKind CreateEntityKind(Point spriteLocation)
        {
            var entityKind = new InternalEntityKind(spriteLocation);
            return entityKind;
        }

        public override ModrogApi.Server.TileKind CreateTileKind(ModrogApi.MapLayer layer, Point spriteLocation)
        {
            var tileKind = new InternalTileKind((short)TileKindsPerLayer[(int)layer].Count, spriteLocation);
            TileKindsPerLayer[(int)layer].Add(tileKind);
            return tileKind;
        }

        public override ModrogApi.Server.World CreateWorld()
        {
            var world = new InternalWorld(this);
            _worlds.Add(world);
            return world;
        }
        #endregion
    }
}
