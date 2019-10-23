using ModrogApi;
using ModrogApi.Server;
using SwarmBasics.Math;
using System.Collections.Generic;

class ScenarioScript : IScenarioScript
{
    public readonly Universe Universe;

    public readonly World ForestWorld;
    public readonly World UndergroundWorld;

    public readonly EntityKind KnightEntityKind;
    public readonly Player Player;

    public readonly Entity Knight;
    public ScenarioScript(Universe universe)
    {
        Universe = universe;

        Universe.LoadTileSet("Main.tileset");

        ForestWorld = Universe.CreateWorld();
        ForestWorld.InsertMap(0, 0, Universe.LoadMap("Maps/Forest.map"));

        UndergroundWorld = Universe.CreateWorld();
        UndergroundWorld.InsertMap(0, 0, Universe.LoadMap("Maps/Underground.map"));

        KnightEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(0, 1));
        KnightEntityKind.SetManualControlScheme(ManualControlScheme.Default);
        KnightEntityKind.SetCapabilities(EntityCapabilities.Move);

        Player = Universe.GetPlayers()[0];
        Knight = ForestWorld.SpawnEntity(KnightEntityKind, new Point(8, 8), EntityDirection.Down, owner: Player);

        // TODO: Build an API that allows taking control of player cameras for cutscenes?
        Player.Teleport(ForestWorld, new Point(8, 8));
        Player.ShowTip("Enter the crypt! Select the Knight and move it with WASD.");
    }

    public void Tick()
    {
        if (Knight.GetWorld() == ForestWorld)
        {
            if (Knight.GetPosition() == new Point(13, -16))
            {
                // Knight has reached the crypt entrance
                // Teleport the knight and the player
                Knight.Teleport(UndergroundWorld, new Point(-3, 2), EntityDirection.Up);
                Player.Teleport(UndergroundWorld, new Point(-3, 2));

                // TODO: Spawn other entities I suppose?
            }
        }
        else
        {
            // TODO: Detect when puzzles are being completed and trigger doors being open / enemies spawning, etc.
        }
    }
}
