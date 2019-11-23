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

    public readonly CharacterKind KnightCharacterKind;
    public readonly CharacterKind RobotCharacterKind;
    public readonly CharacterKind SkeletonCharacterKind;
    public readonly CharacterKind ArrowKind;

    public readonly ItemKind SwordItemKind;
    public readonly ItemKind PistolItemKind;

    public readonly Map ForestMap;
    public readonly Map UndergroundMap;

    public readonly Player Player;

    readonly Point _forestStartCoords = new Point(-60, -25);
    readonly Point _cryptEntranceCoords = new Point(13, -10);

    readonly Point _forestToCryptStairsCoords = new Point(13, -16);
    readonly Point _cryptToForestStairsCoords = new Point(-4, -1);

    public World ForestWorld;
    public World UndergroundWorld;

    public Entity Knight;
    public Entity Robot;
    readonly List<Entity> _skeletons = new List<Entity>();

    readonly Random _random = new Random();

    public ScenarioScript(Universe universe)
    {
        Universe = universe;
        Universe.LoadTileSet("Main.tileset");

        KnightCharacterKind = Universe.CreateCharacterKind(spriteLocation: new Point(0, 6), health: 8);
        RobotCharacterKind = Universe.CreateCharacterKind(spriteLocation: new Point(5, 6), health: 5);
        SkeletonCharacterKind = Universe.CreateCharacterKind(spriteLocation: new Point(0, 8), health: 3);

        SwordItemKind = Universe.CreateItemKind(spriteLocation: new Point(1, 9));
        PistolItemKind = Universe.CreateItemKind(spriteLocation: new Point(4, 10));

        ForestMap = Universe.LoadMap("Maps/Forest.map");
        UndergroundMap = Universe.LoadMap("Maps/Underground.map");

        Player = Universe.GetPlayers()[0];

        Reset();

        // Player.ShowTip("The crypt is not very far... To walk your knight there, click on it then use WASD.");
    }

    void Reset()
    {
        UndergroundWorld?.Destroy();
        ForestWorld?.Destroy();

        ForestWorld = Universe.CreateWorld();
        ForestWorld.InsertMap(0, 0, ForestMap);

        UndergroundWorld = Universe.CreateWorld();
        UndergroundWorld.InsertMap(0, 0, UndergroundMap);

        var spawnCoords = _forestStartCoords;

        // Forest
        var sword = ForestWorld.SpawnItem(SwordItemKind, spawnCoords + new Point(-3, -3));
        var pistol = ForestWorld.SpawnItem(PistolItemKind, spawnCoords + new Point(0, -5));

        _skeletons.Clear();
        _skeletons.Add(ForestWorld.SpawnCharacter(SkeletonCharacterKind, spawnCoords + new Point(10, 3), owner: null));
        _skeletons.Add(ForestWorld.SpawnCharacter(SkeletonCharacterKind, spawnCoords + new Point(12, 4), owner: null));
        foreach (var skeleton in _skeletons) skeleton.SetItem(0, SwordItemKind);

        Knight = ForestWorld.SpawnCharacter(KnightCharacterKind, spawnCoords, owner: Player);
        Player.Teleport(ForestWorld, spawnCoords);
        SetupForestView();
    }

    public void OnCharacterIntent(Entity entity, CharacterIntent intent, Direction direction, int slot, out bool preventDefault)
    {
        preventDefault = false;

        if (intent == CharacterIntent.Use)
        {
            var item = entity.GetItem(0);

            // TODO: Remove this
            if (item == null && entity.GetCharacterKind() == KnightCharacterKind) 
            {
                item = PistolItemKind;
                entity.SetItem(0, item);
            }

            var position = entity.GetPosition();
            var offset = ModrogApi.MathHelper.GetOffsetFromDirection(direction);

            // entity.GetWorld().SpawnEffect(PistolBlastEffectKind, position + offset);
            var range = item == PistolItemKind ? 3 : 1;

            for (var i = 0; i < range; i++)
            {
                position += offset;
                var entities = entity.GetWorld().GetEntities(position);

                foreach (var otherEntity in entities)
                {
                    if (otherEntity.GetCharacterKind() != null)
                    {
                        var health = otherEntity.GetHealth() - 1;
                        otherEntity.SetHealth(health);
                        if (health <= 0)
                        {
                            otherEntity.Remove();
                            if (otherEntity == Knight)
                            {
                                Reset();
                                return;
                            }
                        }
                    }
                }
            }

            return;
        }
    }

    public void Tick()
    {
        if (Knight.GetWorld() == ForestWorld)
        {
            var knightPosition = Knight.GetPosition();

            if (knightPosition == _forestToCryptStairsCoords)
            {
                SetupCryptView();
                Knight.Teleport(UndergroundWorld, _cryptToForestStairsCoords, Direction.Up);
                Knight.SetMoveIntent(Direction.Up);
                Player.Teleport(UndergroundWorld, _cryptToForestStairsCoords + new Point(0, -1));
            }
            else
            {
                foreach (var skeleton in _skeletons)
                {
                    if (_random.Next(2) == 0)
                    {
                        var diff = skeleton.GetPosition() - knightPosition;
                        var manhattanDistance = Math.Abs(diff.X) + Math.Abs(diff.Y);
                        var direction = (Math.Abs(diff.X) > Math.Abs(diff.Y)) ?
                            (diff.X > 0 ? Direction.Left : Direction.Right) :
                            (diff.Y > 0 ? Direction.Up : Direction.Down);

                        if (manhattanDistance > 1) skeleton.SetMoveIntent(direction);
                        else skeleton.SetUseIntent(direction, 0);
                    }
                }
            }
        }
        else
        {
            if (Knight.GetPosition() == _cryptToForestStairsCoords)
            {
                SetupForestView();
                Knight.Teleport(ForestWorld, _forestToCryptStairsCoords, Direction.Down);
                Knight.SetMoveIntent(Direction.Down);
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
