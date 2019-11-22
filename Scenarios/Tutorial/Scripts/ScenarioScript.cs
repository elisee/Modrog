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

    public readonly CharacterKind KnightCharacterKind;
    public readonly CharacterKind RobotCharacterKind;
    public readonly CharacterKind SkeletonCharacterKind;
    public readonly CharacterKind ArrowKind;

    public readonly ItemKind SwordItemKind;
    public readonly ItemKind PistolItemKind;

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

        KnightCharacterKind = Universe.CreateCharacterKind(spriteLocation: new Point(0, 6));
        RobotCharacterKind = Universe.CreateCharacterKind(spriteLocation: new Point(5, 6));
        SkeletonCharacterKind = Universe.CreateCharacterKind(spriteLocation: new Point(0, 8));

        SwordItemKind = Universe.CreateItemKind(spriteLocation: new Point(1, 9));
        PistolItemKind = Universe.CreateItemKind(spriteLocation: new Point(4, 10));

        Player = Universe.GetPlayers()[0];

        var spawnCoords = _forestStartCoords;
        // spawnCoords = _cryptEntranceCoords;

        // Forest
        Knight = ForestWorld.SpawnCharacter(KnightCharacterKind, spawnCoords, owner: Player);

        var sword = ForestWorld.SpawnItem(SwordItemKind, spawnCoords + new Point(-3, -3));
        var pistol = ForestWorld.SpawnItem(PistolItemKind, spawnCoords + new Point(0, -5));

        _skeletons.Add(ForestWorld.SpawnCharacter(SkeletonCharacterKind, spawnCoords + new Point(10, 3), owner: null));
        _skeletons.Add(ForestWorld.SpawnCharacter(SkeletonCharacterKind, spawnCoords + new Point(12, 4), owner: null));

        SetupForestView();
        Player.Teleport(ForestWorld, spawnCoords);
        Player.ShowTip("The crypt is not very far... To walk your knight there, click on it then use WASD.");

        // Crypt
    }

    public void OnCharacterIntent(Entity entity, CharacterIntent intent, Direction direction, int slot, out bool preventDefault)
    {
        preventDefault = false;

        if (intent == CharacterIntent.Use)
        {
            if (entity.GetItem(0) == null) entity.SetItem(0, PistolItemKind);

            var position = entity.GetPosition();
            var offset = ModrogApi.MathHelper.GetOffsetFromDirection(direction);

            // entity.GetWorld().SpawnEffect(PistolBlastEffectKind, position + offset);

            for (var i = 0; i < 3; i++)
            {
                position += offset;
                var entities = entity.GetWorld().GetEntities(position);

                foreach (var otherEntity in entities)
                {
                    if (otherEntity.GetCharacterKind() == SkeletonCharacterKind) otherEntity.Remove();
                }
            }

            return;
        }
    }

    public void Tick()
    {
        if (Knight.GetWorld() == ForestWorld)
        {
            if (Knight.GetPosition() == _forestToCryptStairsCoords)
            {
                // TODO: Would be best if this happened at the start of the next tick
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
