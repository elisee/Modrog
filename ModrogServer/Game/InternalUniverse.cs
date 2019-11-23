using Microsoft.CodeAnalysis;
using ModrogCommon;
using ModrogCommon.Scripting;
using SwarmBasics.Math;
using SwarmBasics.Packets;
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

        internal readonly List<InternalCharacterKind> CharacterKinds = new List<InternalCharacterKind>();
        internal readonly List<InternalItemKind> ItemKinds = new List<InternalItemKind>();

        internal readonly List<InternalTileKind>[] TileKindsPerLayer = new List<InternalTileKind>[(int)ModrogApi.MapLayer.Count];
        internal readonly List<InternalWorld> _worlds = new List<InternalWorld>();
        readonly List<InternalWorld> _addedWorlds = new List<InternalWorld>();

        internal string ScenarioPath { get; private set; }
        internal string SpritesheetPath { get; private set; } = "Spritesheet.png";

        internal int TileSize { get; private set; }

        ScriptContext _scriptContext;
        internal ModrogApi.Server.IScenarioScript _script;

        internal int TickIndex { get; private set; } = -1;

        int _nextCharacterKindId = 0;
        int _nextItemKindId = 0;

        int _nextEntityId = 0;
        internal int GetNextEntityId() => _nextEntityId++;

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
                throw new Exception($"Failed to build scripts for scenario.");
            }

            var scenarioScriptType = _scriptContext.Assembly.GetType("ScenarioScript");
            if (scenarioScriptType == null) throw new Exception("Could not find ScenarioScript class in scripts.");

            var scenarioScriptConstructor = scenarioScriptType.GetConstructor(new Type[] { typeof(ModrogApi.Server.Universe) });
            if (scenarioScriptConstructor == null) throw new Exception($"Could not find ScenarioScript({typeof(ModrogApi.Server.Universe).FullName}) constructor in scripts.");

            try
            {
                _script = (ModrogApi.Server.IScenarioScript)scenarioScriptConstructor.Invoke(new object[] { this });
            }
            catch (Exception exception)
            {
                Console.WriteLine("Encountered exception in ScenarioScript constructor:");
                Console.WriteLine(exception.InnerException);
                throw new Exception("Encountered exception in ScenarioScript constructor.");
            }
        }

        public void Dispose()
        {
            _script = null;
            _scriptContext.Dispose();
            _scriptContext = null;
        }

        internal void Tick()
        {
            TickIndex++;

            foreach (var world in _worlds) world.PreTick();
            _script.Tick();
            foreach (var world in _worlds) world.Tick();
            foreach (var world in _worlds) world.PostTick();

            _worlds.AddRange(_addedWorlds);
            _addedWorlds.Clear();
            _worlds.RemoveAll(x => x.Destroyed);
        }

        #region API
        public override void LoadTileSet(string tileSetPath)
        {
            JsonElement tileSetJson;
            try
            {
                tileSetJson = JsonHelper.Parse(File.ReadAllText(Path.Combine(ScenarioPath, tileSetPath)));

                SpritesheetPath = tileSetJson.GetProperty("spritesheet").GetString();
                TileSize = tileSetJson.GetProperty("tileSize").GetInt32();

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
        public override void SetTileSize(int tileSize) { TileSize = tileSize; }

        public override ModrogApi.Server.TileKind CreateTileKind(ModrogApi.MapLayer layer, Point spriteLocation)
        {
            var tileKind = new InternalTileKind((short)TileKindsPerLayer[(int)layer].Count, spriteLocation);
            TileKindsPerLayer[(int)layer].Add(tileKind);
            return tileKind;
        }

        public override ModrogApi.Server.CharacterKind CreateCharacterKind(Point spriteLocation, int health)
        {
            var characterKind = new InternalCharacterKind(_nextCharacterKindId++, spriteLocation, health);
            CharacterKinds.Add(characterKind);
            return characterKind;
        }

        public override ModrogApi.Server.ItemKind CreateItemKind(Point spriteLocation)
        {
            var itemKind = new InternalItemKind(_nextItemKindId++, spriteLocation);
            ItemKinds.Add(itemKind);
            return itemKind;
        }

        public override ModrogApi.Server.World CreateWorld()
        {
            var world = new InternalWorld(this);
            _addedWorlds.Add(world);
            return world;
        }

        public override ModrogApi.Server.Player[] GetPlayers() => Players;
        #endregion
    }
}
