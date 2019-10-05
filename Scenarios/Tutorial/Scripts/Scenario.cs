using DeepSwarmApi;

class Scenario : DeepSwarmScenario
{
    public readonly DeepSwarmApi DeepSwarm;

    public readonly Tile DirtTile;
    public readonly Tile StoneTile;

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

    public Scenario(DeepSwarmApi deepSwarm)
    {
        DeepSwarm = deepSwarm;

        FloorTile = DeepSwarm.CreateTile(sprite: new Point(0, 0));
        DirtTile = DeepSwarm.CreateTile(sprite: new Point(1, 0));
        StoneTile = DeepSwarm.CreateTile(sprite: new Point(2, 0));

        // TODO: Build the map or load it from a file, whatever
        OutsideWorld = DeepSwarm.CreateWorld(16, 16);
        CryptWorld = DeepSwarm.CreateWorld(64, 64);

        // world.Map.SetTile(i, j, DirtTile); ...

        // TODO: Flesh out API for setting up entity kinds. Should allow selecting frames from a spritesheet
        KnightEntityKind = DeepSwarm.CreateEntityKind(sprite: new Point(0, 1));
        KnightEntityKind.SetManualControlScheme(ManualControlScheme.Default);

        HeavyEntityKind = DeepSwarm.CreateEntityKind(sprite: new Point(0, 2));
        HeavyEntityKind.SetScriptable(true);
        HeavyEntityKind.SetCapabilities(EntityCapabilities.Attack);

        // etc.

        Player = DeepSwarm.Players[0];
        King = OutsideWorld.SpawnEntity(EntityType.King, new Point(32, 32), owner: player);

        // TODO: Build an API that allows taking control of player cameras for cutscenes?
        Player.Teleport(OutsideWorld, new Point(8, 8));

        Player.ShowTip("Enter the crypt! Select the Knight and move it with WASD.");
    }

    public override void Tick()
    {
        if (Player.World == OutsideWorld)
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
        }
    }
}
