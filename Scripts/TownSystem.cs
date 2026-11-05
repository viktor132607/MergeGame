using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

public enum MaterialKind { Wood, Bricks, Glass, Hardware }

public sealed class BuildingProgress
{
    public int BuildingId { get; set; }
    public int Stage { get; set; }
}

public sealed class TownState
{
    public int[] Materials { get; set; } = new int[4];
    public List<BuildingProgress> Buildings { get; set; } = new();
    public int SelectedBuilding { get; set; }
    public int TotalConstructionStages { get; set; }
}

public readonly record struct ConstructionCost(int Coins, int Wood, int Bricks, int Glass, int Hardware)
{
    public int GetMaterial(int index) => index switch
    {
        0 => Wood, 1 => Bricks, 2 => Glass, 3 => Hardware, _ => 0
    };
}

public readonly record struct BuildingDefinition(int Id, string Name, string Description, int UnlockLevel);

public static class TownCatalog
{
    public const int MaxStage = 3;
    public const int BuildingCount = 20;
    public static readonly string[] MaterialNames = { "Wood", "Bricks", "Glass", "Hardware" };
    public static readonly string[] MaterialIcons = { "wood", "bricks", "glass", "hardware" };
    public static readonly BuildingDefinition[] Buildings =
    {
        new(0, "Beach Cafe", "A welcoming cafe on the sandy coast.", 1),
        new(1, "Flower Market", "A colorful home for tropical flowers.", 2),
        new(2, "Seaside Bakery", "Fresh pastries overlooking the bay.", 3),
        new(3, "Sunset Hotel", "A beautiful getaway for visitors.", 5),
        new(4, "Ocean Marina", "Boat excursions and a lively dock.", 7),
        new(5, "Island Lighthouse", "A bright beacon above the ocean.", 9),
        new(6, "Tropical Restaurant", "A lively seaside dining terrace.", 11),
        new(7, "Fruit Orchard", "A grove full of sweet tropical fruit.", 13),
        new(8, "Ice Cream Parlor", "Frozen treats and colorful scoops.", 15),
        new(9, "Souvenir Shop", "Handcrafted gifts for island visitors.", 17),
        new(10, "Beach Bungalows", "Cozy cottages on the waterfront.", 19),
        new(11, "Fishing Pier", "A wooden pier for fishing boats.", 21),
        new(12, "Botanical Garden", "Flowers and shaded island paths.", 23),
        new(13, "Music Pavilion", "Open-air stage for coastal concerts.", 25),
        new(14, "Surf School", "Boards, lessons and ocean adventures.", 27),
        new(15, "Spa Retreat", "Relaxing resort with garden pools.", 29),
        new(16, "Aquarium", "See the colorful creatures of the reef.", 31),
        new(17, "Adventure Park", "Climbing, games and rides for visitors.", 33),
        new(18, "Island Museum", "Discover the history of the island.", 35),
        new(19, "Grand Resort", "The most luxurious destination on the island.", 40)
    };

    // The original six buildings retain precisely the same prices for saved games.
    private static readonly ConstructionCost[,] LegacyCost =
    {
        { new(100, 5, 3, 0, 1), new(240, 10, 8, 3, 3), new(480, 16, 13, 7, 6) },
        { new(140, 6, 3, 1, 1), new(300, 11, 9, 4, 4), new(580, 18, 14, 9, 7) },
        { new(170, 6, 5, 2, 2), new(370, 13, 11, 5, 4), new(690, 21, 17, 10, 8) },
        { new(250, 10, 7, 4, 3), new(520, 18, 15, 9, 6), new(1000, 28, 24, 15, 10) },
        { new(320, 14, 8, 5, 4), new(650, 22, 17, 10, 7), new(1200, 33, 28, 16, 11) },
        { new(450, 15, 12, 8, 5), new(800, 26, 22, 14, 8), new(1500, 40, 33, 23, 14) }
    };

    public static ConstructionCost GetCost(int id, int nextStage)
    {
        if (id < 0 || id >= BuildingCount || nextStage < 1 || nextStage > MaxStage)
            throw new ArgumentOutOfRangeException();
        if (id < 6) return LegacyCost[id, nextStage - 1];
        int index = id - 6;
        int multiplier = nextStage;
        return new ConstructionCost(
            (220 + index * 85) * multiplier * multiplier,
            (7 + index * 2) * multiplier,
            (5 + index * 2) * multiplier,
            (2 + index) * multiplier,
            (2 + (index * 3) / 2) * multiplier);
    }
}

public sealed class TownSystem
{
    private readonly GameData _game;
    private readonly Random _random = new();

    public TownSystem(GameData game) => _game = game;
    public TownState State => _game.State.Town;

    public int Stage(int buildingId) =>
        State.Buildings.FirstOrDefault(b => b.BuildingId == buildingId)?.Stage ?? 0;

    public bool Unlocked(int buildingId) => buildingId >= 0 && buildingId < TownCatalog.BuildingCount &&
        _game.State.Level >= TownCatalog.Buildings[buildingId].UnlockLevel;

    public bool CanBuild(int id)
    {
        if (!Unlocked(id) || Stage(id) >= TownCatalog.MaxStage) return false;
        var cost = TownCatalog.GetCost(id, Stage(id) + 1);
        return _game.State.Coins >= cost.Coins &&
               Enumerable.Range(0, 4).All(i => State.Materials[i] >= cost.GetMaterial(i));
    }

    public bool Construct(int id)
    {
        if (id < 0 || id >= TownCatalog.BuildingCount) return false;
        if (!Unlocked(id))
        {
            _game.SetMessage("Unlock " + TownCatalog.Buildings[id].Name +
                " at level " + TownCatalog.Buildings[id].UnlockLevel + ".");
            return false;
        }
        int current = Stage(id);
        if (current >= TownCatalog.MaxStage)
        {
            _game.SetMessage(TownCatalog.Buildings[id].Name + " is fully upgraded.");
            return false;
        }
        var cost = TownCatalog.GetCost(id, current + 1);
        if (!CanBuild(id))
        {
            _game.SetMessage("More coins or construction materials are needed for " +
                TownCatalog.Buildings[id].Name + ".");
            return false;
        }

        _game.State.Coins -= cost.Coins;
        for (int i = 0; i < 4; i++) State.Materials[i] -= cost.GetMaterial(i);
        var progress = State.Buildings.FirstOrDefault(b => b.BuildingId == id);
        if (progress == null) State.Buildings.Add(new BuildingProgress { BuildingId = id, Stage = 1 });
        else progress.Stage++;
        State.TotalConstructionStages++;
        int stage = Stage(id);
        _game.GrantAlbumReward(0, stage * 30);
        // Celebrate the upgrade with an additional free card pack.
        State.SelectedBuilding = id;
        _game.State.Album.Packs++;
        _game.SetMessage(TownCatalog.Buildings[id].Name + " built to stage " + stage +
            "! Bonus XP and a card pack awarded.");
        return true;
    }

    public void OnOrderCompleted()
    {
        // Every fulfilled order provides guaranteed building materials.
        int primary = _random.Next(4);
        State.Materials[primary] += 2;
        State.Materials[(primary + 1) % 4]++;
    }

    public void OnMergeCompleted()
    {
        // A material for every three merges (energy is still unlimited).
        if (_game.State.TotalMerges % 3 == 0)
            State.Materials[_random.Next(4)]++;
    }

    public static void Normalize(TownState town)
    {
        if (town.Materials == null || town.Materials.Length != 4)
        {
            var valid = town.Materials ?? Array.Empty<int>();
            town.Materials = new int[4];
            Array.Copy(valid, town.Materials, Math.Min(4, valid.Length));
        }
        for (int i = 0; i < 4; i++) town.Materials[i] = Math.Clamp(town.Materials[i], 0, 1000000);
        town.Buildings = (town.Buildings ?? new List<BuildingProgress>())
            .Where(b => b != null && b.BuildingId >= 0 && b.BuildingId < TownCatalog.BuildingCount)
            .GroupBy(b => b.BuildingId)
            .Select(g => new BuildingProgress { BuildingId = g.Key,
                Stage = Math.Clamp(g.Max(x => x.Stage), 0, TownCatalog.MaxStage) })
            .OrderBy(b => b.BuildingId).ToList();
        town.SelectedBuilding = Math.Clamp(town.SelectedBuilding, 0, TownCatalog.BuildingCount - 1);
        town.TotalConstructionStages = Math.Max(0, town.TotalConstructionStages);
    }
}
