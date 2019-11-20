using ModrogApi;
using ModrogApi.Server;
using SwarmBasics.Math;
using System;
using System.Collections.Generic;

class ScenarioScript: IScenarioScript
{
    public readonly Universe Universe;

    public ScenarioScript(Universe universe)
    {
        Universe = universe;
        Universe.LoadTileSet("Main.tileset");

        var world = Universe.CreateWorld();
        world.InsertMap(0, 0, Universe.LoadMap("Maps/Test.map"));

        var player = Universe.GetPlayers()[0];
        var characterEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(0, 0));
        var character = world.SpawnEntity(characterEntityKind, new Point(0, 0), owner: player);
        player.Teleport(world, new Point(0, 0));
    }

    public void OnEntityIntent(Entity entity, EntityIntent intent, Direction direction, int slot, out bool preventDefault)
    {
        preventDefault = false;
    }

    public void Tick()
    {
    }
}
