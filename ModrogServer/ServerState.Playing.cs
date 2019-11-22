using ModrogCommon;
using SwarmBasics.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ModrogServer
{
    partial class ServerState
    {
        void StartPlaying()
        {
            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Chat);
            _packetWriter.WriteByteSizeString("");
            _packetWriter.WriteByteSizeString($"Loading scenario...");
            Broadcast();

            var peers = new Peer[_identifiedPeerSockets.Count];
            var players = new Game.InternalPlayer[peers.Length];

            for (var i = 0; i < peers.Length; i++)
            {
                peers[i] = _peersBySocket[_identifiedPeerSockets[i]];
                peers[i].Identity.PlayerIndex = i;
                players[i] = new Game.InternalPlayer { Index = i, Name = peers[i].Identity.Name };

                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.SetPlayerIndex);
                _packetWriter.WriteInt(peers[i].Identity.PlayerIndex);
                Send(peers[i].Socket);
            }

            BroadcastPeerList();

            var scenarioPath = Path.Combine(_scenariosPath, _scenarioName);
            _universe = new Game.InternalUniverse(players, scenarioPath);
            _stage = ServerStage.Playing;

            _spritesheetBytes = File.ReadAllBytes(Path.Combine(scenarioPath, _universe.SpritesheetPath));

            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.UniverseSetup);
            WriteUniverseSetup();
            Broadcast();
        }

        void WriteUniverseSetup()
        {
            _packetWriter.WriteInt(_spritesheetBytes.Length);
            _packetWriter.WriteBytes(_spritesheetBytes);
            _packetWriter.WriteShort((short)_universe.TileSize);

            for (var layer = 0; layer < _universe.TileKindsPerLayer.Length; layer++)
            {
                _packetWriter.WriteShort((short)_universe.TileKindsPerLayer[layer].Count);
                foreach (var tileKind in _universe.TileKindsPerLayer[layer]) _packetWriter.WriteShortPoint(tileKind.SpriteLocation);
            }

            _packetWriter.WriteShort((short)_universe.CharacterKinds.Count);
            foreach (var kind in _universe.CharacterKinds)
            {
                _packetWriter.WriteShortPoint(kind.SpriteLocation);
            }

            _packetWriter.WriteShort((short)_universe.ItemKinds.Count);
            foreach (var kind in _universe.ItemKinds)
            {
                _packetWriter.WriteShortPoint(kind.SpriteLocation);
            }
        }

        void Tick()
        {
            _universe.Tick();

            // Gather new data to send to players
            foreach (var socket in _identifiedPeerSockets)
            {
                var peer = _peersBySocket[socket];
                var player = _universe.Players[peer.Identity.PlayerIndex];
                var seenEntities = new HashSet<Game.InternalEntity>();
                var seenDirtyEntities = new HashSet<Game.InternalEntity>();
                var seenTileStacks = new Dictionary<Point, short[]>();

                foreach (var ownedEntity in player.OwnedEntities)
                {
                    var world = ownedEntity.World;
                    if (world != player.World) continue;

                    var squaredViewRadius = ownedEntity.ViewRadius * ownedEntity.ViewRadius;
                    var viewRadius = ownedEntity.ViewRadius;

                    for (var dy = -viewRadius; dy <= viewRadius; dy++)
                    {
                        for (var dx = -viewRadius; dx <= viewRadius; dx++)
                        {
                            var target = new Point(ownedEntity.Position.X + dx, ownedEntity.Position.Y + dy);

                            var squaredDistance = dx * dx + dy * dy;
                            var isInView = squaredDistance <= squaredViewRadius;
                            if (!isInView) continue;

                            var hasLineOfSight =
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X, target.Y) ||
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X - Math.Sign(dx), target.Y) ||
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X, target.Y - Math.Sign(dy));
                            if (!hasLineOfSight) continue;

                            seenTileStacks.TryAdd(target, world.PeekTileStack(target));

                            foreach (Game.InternalEntity entityInView in world.GetEntities(target))
                            {
                                seenEntities.Add(entityInView);
                                if (entityInView.Action != ModrogApi.EntityAction.Idle || entityInView.AreItemSlotsDirty) seenDirtyEntities.Add(entityInView);
                            }
                        }
                    }
                }

                // Send seen state to player
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Tick);
                _packetWriter.WriteInt(_universe.TickIndex);

                _packetWriter.WriteByte(player.WasJustTeleported ? (byte)1 : (byte)0);

                if (player.WasJustTeleported)
                {
                    _packetWriter.WriteShortPoint(player.Position);
                    player.WasJustTeleported = false;
                }

                // Out-of-sight entities
                var outOfSightEntities = new HashSet<Game.InternalEntity>(player.EntitiesInSight);
                outOfSightEntities.ExceptWith(seenEntities);
                _packetWriter.WriteShort((short)outOfSightEntities.Count);
                foreach (var entity in outOfSightEntities) _packetWriter.WriteInt(entity.Id);

                var newlySeenEntities = new HashSet<Game.InternalEntity>(seenEntities);
                newlySeenEntities.ExceptWith(player.EntitiesInSight);

                // Newly seen entities
                WriteNewEntitiesInSight(newlySeenEntities, player.Index);

                // Entity actions
                player.EntitiesInSight.Clear();
                player.EntitiesInSight.UnionWith(seenEntities);

                _packetWriter.WriteShort((short)seenDirtyEntities.Count);

                foreach (var entity in seenDirtyEntities)
                {
                    Debug.Assert(entity.World == player.World);

                    _packetWriter.WriteInt(entity.Id);
                    _packetWriter.WriteByte((byte)entity.Action);
                    _packetWriter.WriteByte((byte)entity.ActionDirection);
                    _packetWriter.WriteShort((short)(entity.ActionItem?.Id ?? -1));

                    if (entity.PlayerIndex == player.Index)
                    {
                        _packetWriter.WriteByte((byte)(entity.AreItemSlotsDirty ? 1 : 0));
                        if (entity.AreItemSlotsDirty)
                        {
                            foreach (var itemKind in entity.ItemSlots) _packetWriter.WriteShort((short)(itemKind?.Id ?? -1));
                        }
                    }
                }

                _packetWriter.WriteShort((short)seenTileStacks.Count);
                foreach (var (position, tileStack) in seenTileStacks)
                {
                    _packetWriter.WriteShortPoint(position);
                    for (var i = 0; i < tileStack.Length; i++) _packetWriter.WriteShort(tileStack[i]);
                }

                Send(peer.Socket);
            }
        }
    }
}
