using DeepSwarmApi.Server;
using DeepSwarmBasics.Math;
using System.Collections.Generic;

class ScenarioScript : IScenarioScript
{
    public readonly Universe Universe;

    public readonly TileKind FloorTileKind;
    public readonly TileKind DirtTileKind;
    public readonly TileKind StoneTileKind;

    // Knight can fight enemies
    public readonly EntityKind KnightEntityKind;
    // Heavy can unlock doors and push heavy stuff
    public readonly EntityKind HeavyEntityKind;
    // Robot can dig
    public readonly EntityKind RobotEntityKind;

    public readonly World OutsideWorld;
    public readonly World CryptWorld;
    public readonly Player Player;

    // Friendlies
    public readonly Entity Knight;
    public readonly Entity Heavy;
    public readonly Entity Robot;

    public readonly Entity Barracks;
    public readonly List<Entity> Footmen = new List<Entity>();

    // Enemies
    public readonly List<Entity> Zombies = new List<Entity>();

    public ScenarioScript(Universe universe)
    {
        Universe = universe;

        FloorTileKind = Universe.CreateTileKind(spriteLocation: new Point(0, 0), TileFlags.None);
        DirtTileKind = Universe.CreateTileKind(spriteLocation: new Point(1, 0), TileFlags.Solid | TileFlags.Opaque);
        StoneTileKind = Universe.CreateTileKind(spriteLocation: new Point(2, 0), TileFlags.Solid | TileFlags.Opaque);

        // TODO: Build the map or load it from a file, whatever
        OutsideWorld = Universe.CreateWorld(16, 16);
        CryptWorld = Universe.CreateWorld(64, 64);

        for (var j = 0; j < 16; j++)
        {
            for (var i = 0; i < 16; i++)
            {
                if (i == 0 || j == 0 || i == 15 || j == 15) OutsideWorld.SetTile(i, j, DirtTileKind);
                else OutsideWorld.SetTile(i, j, FloorTileKind);
            }
        }

        // world.Map.SetTile(i, j, DirtTileKind); ...

        KnightEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(0, 1));
        KnightEntityKind.SetManualControlScheme(ManualControlScheme.Default);
        KnightEntityKind.SetCapabilities(EntityCapabilities.Move);

        HeavyEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(0, 2));
        HeavyEntityKind.SetScriptable(true);
        HeavyEntityKind.SetCapabilities(EntityCapabilities.Move | EntityCapabilities.Attack | EntityCapabilities.Push);

        Player = Universe.GetPlayers()[0];
        Knight = OutsideWorld.SpawnEntity(KnightEntityKind, new Point(8, 8), EntityDirection.Down, owner: Player);

        // TODO: Build an API that allows taking control of player cameras for cutscenes?
        Player.Teleport(OutsideWorld, new Point(8, 8));
        Player.ShowTip("Enter the crypt! Select the Knight and move it with WASD.");
    }

    public void Tick()
    {
        /*if (Player.World == OutsideWorld)
        {
            if (Player.Position == new Point(12, 12))
            {
                // Player has reached the crypt entrance
                Player.Teleport(CryptWorld, new Point(16, 16));

                // TODO: Spawn other entities I suppose?
            }
        }
        else
        {
            // TODO: Detect when puzzles are being completed and trigger doors being open / enemies spawning, etc.
        }*/
    }
}
