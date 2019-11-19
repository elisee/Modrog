using ModrogApi;
using ModrogApi.Server;
using SwarmBasics.Math;
using System;
using System.Collections.Generic;

/*
    TODO:
        - Text to explain the context
        - Enemies in the forest
        - Robot
*/

class ScenarioScript : IScenarioScript
{
    public readonly Universe Universe;

    public readonly World ForestWorld;
    public readonly World UndergroundWorld;

    public readonly Player Player;

    public readonly EntityKind KnightEntityKind;
    public readonly EntityKind RobotEntityKind;
    public readonly EntityKind SkeletonEntityKind;

    public readonly Entity Knight;
    public readonly Entity Robot;
    readonly List<Entity> _skeletons = new List<Entity>();

    readonly Point _forestStartCoords = new Point(-60, -25);
    readonly Point _cryptEntranceCoords = new Point(13, -10);

    readonly Point _forestToCryptStairsCoords = new Point(13, -16);
    readonly Point _cryptToForestStairsCoords = new Point(-4, -1);

    public ScenarioScript(Universe universe)
    {
        Universe = universe;

        Universe.LoadTileSet("Main.tileset");

        ForestWorld = Universe.CreateWorld();
        ForestWorld.InsertMap(0, 0, Universe.LoadMap("Maps/Forest.map"));

        UndergroundWorld = Universe.CreateWorld();
        UndergroundWorld.InsertMap(0, 0, Universe.LoadMap("Maps/Underground.map"));

        KnightEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(0, 6));
        KnightEntityKind.SetManualControlScheme(ManualControlScheme.Default);
        KnightEntityKind.SetCapabilities(EntityCapabilities.Move);

        RobotEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(4, 6));
        RobotEntityKind.SetManualControlScheme(ManualControlScheme.Default);
        RobotEntityKind.SetCapabilities(EntityCapabilities.Move);

        SkeletonEntityKind = Universe.CreateEntityKind(spriteLocation: new Point(0, 8));

        Player = Universe.GetPlayers()[0];

        var spawnCoords = _forestStartCoords;
        // spawnCoords = _cryptEntranceCoords;

        // Forest
        Knight = ForestWorld.SpawnEntity(KnightEntityKind, spawnCoords, owner: Player);

        _skeletons.Add(ForestWorld.SpawnEntity(SkeletonEntityKind, spawnCoords + new Point(10, 3), owner: null));
        _skeletons.Add(ForestWorld.SpawnEntity(SkeletonEntityKind, spawnCoords + new Point(12, 4), owner: null));

        SetupForestView();
        Player.Teleport(ForestWorld, spawnCoords);
        Player.ShowTip("The crypt is not very far... To walk your knight there, click on it then use WASD.");

        // Crypt
    }

    public void Tick()
    {
        if (Knight.GetWorld() == ForestWorld)
        {
            if (Knight.GetPosition() == _forestToCryptStairsCoords)
            {
                SetupCryptView();
                Knight.Teleport(UndergroundWorld, _cryptToForestStairsCoords + new Point(0, -1));
                Player.Teleport(UndergroundWorld, _cryptToForestStairsCoords + new Point(0, -1));
            }

            foreach (var skeleton in _skeletons)
            {
                // skeleton.PlanMove(EntityMove.Forward);
            }
        }
        else
        {
            if (Knight.GetPosition() == _cryptToForestStairsCoords)
            {
                SetupForestView();
                Knight.Teleport(ForestWorld, _forestToCryptStairsCoords + new Point(0, 1));
                Player.Teleport(ForestWorld, _forestToCryptStairsCoords + new Point(0, 1));
            }
        }
    }

    void SetupForestView()
    {
        Knight.SetView(16);
    }

    void SetupCryptView()
    {
        Knight.SetView(8);
    }
}
