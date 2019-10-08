﻿using DeepSwarmBasics.Math;
using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeepSwarmServer
{
    partial class ServerState
    {

        void StartPlaying()
        {
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

            _spritesheetBytes = File.ReadAllBytes(Path.Combine(scenarioPath, "Spritesheet.png"));

            _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Spritesheet);
            _packetWriter.WriteInt(_spritesheetBytes.Length);
            _packetWriter.WriteBytes(_spritesheetBytes);
            Broadcast();
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
                var seenTiles = new Dictionary<Point, short>();

                foreach (var ownedEntity in player.OwnedEntities)
                {
                    var world = ownedEntity.World;
                    if (world != player.World) continue;

                    var squaredOmniViewRadius = ownedEntity.OmniViewRadius * ownedEntity.OmniViewRadius;
                    var squaredDirectionalViewRadius = ownedEntity.DirectionalViewRadius * ownedEntity.DirectionalViewRadius;
                    var directionAngle = ownedEntity.GetDirectionAngle();

                    var radius = Math.Max(ownedEntity.OmniViewRadius, ownedEntity.DirectionalViewRadius);
                    seenTiles.TryAdd(ownedEntity.Position, world.PeekTile(ownedEntity.Position.X, ownedEntity.Position.Y));

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
                                Math.Abs(MathHelper.WrapAngle(angle - directionAngle)) < ownedEntity.HalfFieldOfView);

                            if (!isInView) continue;

                            var target = new Point(ownedEntity.Position.X + dx, ownedEntity.Position.Y + dy);

                            var hasLineOfSight =
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X, target.Y) ||
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X - Math.Sign(dx), target.Y) ||
                                world.HasLineOfSight(ownedEntity.Position.X, ownedEntity.Position.Y, target.X, target.Y - Math.Sign(dy));
                            if (!hasLineOfSight) continue;

                            seenTiles.TryAdd(target, world.PeekTile(target.X, target.Y));
                            var targetEntity = world.PeekEntity(target.X, target.Y);
                            if (targetEntity != null && targetEntity.PlayerIndex != player.Index) seenEntities.Add(targetEntity);
                        }
                    }
                }

                // Send seen state to player
                _packetWriter.WriteByte((byte)Protocol.ServerPacketType.Tick);
                _packetWriter.WriteInt(_universe.TickIndex);

                _packetWriter.WriteByte(player.WasJustTeleported ? (byte)1 : (byte)0);

                if (player.WasJustTeleported)
                {
                    _packetWriter.WriteShort((short)player.World.Width);
                    _packetWriter.WriteShort((short)player.World.Height);

                    _packetWriter.WriteShort((short)player.Position.X);
                    _packetWriter.WriteShort((short)player.Position.Y);
                    player.WasJustTeleported = false;
                }

                // TODO: Must count only owned entities that are in our world!!
                _packetWriter.WriteShort((short)(player.OwnedEntities.Count + seenEntities.Count));

                foreach (var entity in player.OwnedEntities)
                {
                    if (entity.World != player.World) continue;

                    _packetWriter.WriteInt(entity.Id);
                    _packetWriter.WriteShort((short)entity.Position.X);
                    _packetWriter.WriteShort((short)entity.Position.Y);
                    _packetWriter.WriteByte((byte)entity.Direction);
                    _packetWriter.WriteShort((byte)entity.PlayerIndex);
                }

                foreach (var entity in seenEntities)
                {
                    if (entity.World != player.World) continue;

                    _packetWriter.WriteInt(entity.Id);
                    _packetWriter.WriteShort((short)entity.Position.X);
                    _packetWriter.WriteShort((short)entity.Position.Y);
                    _packetWriter.WriteByte((byte)entity.Direction);
                    _packetWriter.WriteShort((byte)entity.PlayerIndex);
                }

                _packetWriter.WriteShort((short)seenTiles.Count);
                foreach (var (position, tile) in seenTiles)
                {
                    _packetWriter.WriteShort((short)position.X);
                    _packetWriter.WriteShort((short)position.Y);
                    _packetWriter.WriteShort(tile);
                }

                Send(peer.Socket);
            }
        }
    }
}
