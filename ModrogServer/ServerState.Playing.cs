﻿using ModrogCommon;
using SwarmBasics.Math;
using System;
using System.Collections.Generic;
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

            for (var layer = 0; layer < _universe.TileKindsPerLayer.Length; layer++)
            {
                _packetWriter.WriteInt(_universe.TileKindsPerLayer[layer].Count);

                foreach (var tileKind in _universe.TileKindsPerLayer[layer])
                {
                    _packetWriter.WriteShort((short)tileKind.SpriteLocation.X);
                    _packetWriter.WriteShort((short)tileKind.SpriteLocation.Y);
                }
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
                var seenTileStacks = new Dictionary<Point, short[]>();

                foreach (var ownedEntity in player.OwnedEntities)
                {
                    var world = ownedEntity.World;
                    if (world != player.World) continue;

                    seenEntities.Add(ownedEntity);

                    var squaredOmniViewRadius = ownedEntity.OmniViewRadius * ownedEntity.OmniViewRadius;
                    var squaredDirectionalViewRadius = ownedEntity.DirectionalViewRadius * ownedEntity.DirectionalViewRadius;
                    var directionAngle = ownedEntity.GetDirectionAngle();

                    var radius = Math.Max(ownedEntity.OmniViewRadius, ownedEntity.DirectionalViewRadius);
                    seenTileStacks.TryAdd(ownedEntity.Position, world.PeekTileStack(ownedEntity.Position.X, ownedEntity.Position.Y));

                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        for (var dx = -radius; dx <= radius; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            var angle = MathF.Atan2(dy, dx);
                            var squaredDistance = dx * dx + dy * dy;

                            var isInView =
                                squaredDistance <= squaredOmniViewRadius ||
                                (squaredDistance <= squaredDirectionalViewRadius &&
                                Math.Abs(MathHelper.WrapAngle(angle - directionAngle)) < ownedEntity.HalfFieldOfViewAngle);

                            if (!isInView) continue;

                            var target = new Point(ownedEntity.Position.X + dx, ownedEntity.Position.Y + dy);

                            var hasLineOfSight =
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X, target.Y) ||
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X - Math.Sign(dx), target.Y) ||
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X, target.Y - Math.Sign(dy));
                            if (!hasLineOfSight) continue;

                            seenTileStacks.TryAdd(target, world.PeekTileStack(target.X, target.Y));
                            var targetEntity = world.PeekEntity(target.X, target.Y);
                            if (targetEntity != null) seenEntities.Add(targetEntity);
                        }
                    }
                }

                // Send seen state to player
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Tick);
                _packetWriter.WriteInt(_universe.TickIndex);

                _packetWriter.WriteByte(player.WasJustTeleported ? (byte)1 : (byte)0);

                if (player.WasJustTeleported)
                {
                    _packetWriter.WriteShort((short)player.Position.X);
                    _packetWriter.WriteShort((short)player.Position.Y);
                    player.WasJustTeleported = false;
                }

                _packetWriter.WriteShort((short)seenEntities.Count);

                foreach (var entity in seenEntities)
                {
                    if (entity.World != player.World) continue;

                    _packetWriter.WriteInt(entity.Id);
                    _packetWriter.WriteShort((short)entity.SpriteLocation.X);
                    _packetWriter.WriteShort((short)entity.SpriteLocation.Y);
                    _packetWriter.WriteShort((short)entity.Position.X);
                    _packetWriter.WriteShort((short)entity.Position.Y);
                    _packetWriter.WriteByte((byte)entity.Direction);
                    _packetWriter.WriteShort((byte)entity.PlayerIndex);
                }

                _packetWriter.WriteShort((short)seenTileStacks.Count);
                foreach (var (position, tileStack) in seenTileStacks)
                {
                    _packetWriter.WriteShort((short)position.X);
                    _packetWriter.WriteShort((short)position.Y);
                    for (var i = 0; i < tileStack.Length; i++) _packetWriter.WriteShort(tileStack[i]);
                }

                Send(peer.Socket);
            }
        }
    }
}
