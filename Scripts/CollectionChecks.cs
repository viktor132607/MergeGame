using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

public static class CollectionChecks
{
    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException("COLLECTION TEST FAILED: " + message);
    }

    public static void Run()
    {
        var game = new GameData();
        Check(CollectionCatalog.TotalCount == 23, "23 collections");
        Check(CollectionCatalog.TotalItems == 230, "230 merge items");
        Check(CollectionCatalog.Names.Length == 23, "all item chains exist");
        Check(CollectionCatalog.Names.All(chain => chain.Length == 10), "each has ten tiers");
        Check(CollectionCatalog.FamilyNames.Length == 23, "all 23 names");
        Check(CollectionCatalog.GeneratorNames.Length == 23, "all 23 generator names");

        var unique = CollectionCatalog.Names.SelectMany(x => x).ToList();
        Check(unique.Count == 230, "230 item records");
        for (int id = 0; id < TownCatalog.BuildingCount; id++)
            Check(CollectionCatalog.FamilyForBuilding(id) == id + 3 &&
                CollectionCatalog.BuildingForFamily(id + 3) == id, "mapping for building " + id);

        int starter = game.State.Tiles.Count(t => t != null);
        Check(starter == 21, "18 original items and three on-board starter generators");
        Check(CollectionCatalog.UnlockedFamilies(game.Town).Count() == 3, "three starter chains");
        Check(!game.Spawn(3), "cannot generate an unbuilt cafe collection");
        Check(game.State.Tiles.Count(t => t != null) == starter, "locked generators don't alter board");

        // Building zero unlocks its chain and selects it automatically.
        game.State.Coins = 1000000;
        game.State.Town.Materials = new[] { 100000, 100000, 100000, 100000 };
        Check(game.Town.Construct(0), "construct cafe");
        Check(CollectionCatalog.IsUnlocked(3, game.Town), "cafe collection unlocked");
        Check(game.State.SelectedGenerator == 3, "new generator selected after building");
        Check(game.Spawn(3), "generator yields an item");
        Check(game.State.Tiles.Any(t => t != null && t.Family == 3), "spawned item belongs to collection");
        Check(game.SelectGenerator(3), "select unlocked family");

        // Two same-tier tiles must merge into one item in the same collection.
        var tiles = game.State.Tiles;
        int[] free = Enumerable.Range(0, FoodCatalog.CellCount).Where(i => tiles[i] == null).Take(2).ToArray();
        Check(free.Length == 2, "two open spaces for a test merge");
        tiles[free[0]] = new TileData { Family = 3, Tier = 1 };
        tiles[free[1]] = new TileData { Family = 3, Tier = 1 };
        int before = game.State.TotalMerges;
        Check(game.Interact(free[0], free[1]), "merge two cafe tier-one items");
        Check(game.State.TotalMerges == before + 1, "merge increments count");
        Check(tiles[free[1]] != null && tiles[free[1]]!.Family == 3 &&
            tiles[free[1]]!.Tier == 2, "higher-tier item stays in correct collection");

        game.State.Level = 40;
        for (int i = 1; i < TownCatalog.BuildingCount; i++)
            Check(game.Town.Construct(i), "construct building " + i);
        Check(CollectionCatalog.UnlockedFamilies(game.Town).Count() == 23, "all generators unlocked");
        for (int family = 3; family < 23; family++)
        {
            Check(game.SelectGenerator(family), "select new family " + family);
            Check(CollectionCatalog.ItemName(family, 10) != "Unknown", "tier ten defined " + family);
        }

        SaveData snapshot = game.State;
        var restored = new GameData();
        Check(restored.Restore(snapshot), "restore expanded save");
        Check(restored.State.SelectedGenerator == 22, "saved selection restored");
        Check(restored.Town.Stage(19) > 0 && CollectionCatalog.IsUnlocked(22, restored.Town),
            "last family persists after restore");

        var oldSave = new SaveData();
        Check(restored.Restore(oldSave), "restore pre-update save");
        Check(restored.State.SelectedGenerator == 0, "legacy saves default to orchard");
        Check(!CollectionCatalog.IsUnlocked(3, restored.Town), "locked by default");

        Console.WriteLine("COLLECTION TESTS PASSED: 23 chains, 230 items, building unlocks, merges, generators, persistence");
    }
}
