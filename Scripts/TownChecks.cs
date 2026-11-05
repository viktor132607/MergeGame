using System;
using System.Linq;

namespace MergeHarvest;

public static class TownChecks
{
    private static void Check(bool ok, string note)
    {
        if (!ok) throw new InvalidOperationException("TOWN TEST FAILED: " + note);
    }

    public static void Run()
    {
        var game = new GameData();
        var town = game.Town;
        Check(TownCatalog.BuildingCount == 6, "six buildings");
        Check(town.Stage(0) == 0, "new town starts without buildings");
        Check(!town.CanBuild(0), "materials must be collected");
        Check(!town.Unlocked(5), "high-level buildings locked");

        game.State.CompletedOrders = 1;
        town.OnOrderCompleted();
        Check(game.State.Town.Materials.Sum() == 3, "order rewards three materials");
        game.State.TotalMerges = 2;
        town.OnMergeCompleted();
        Check(game.State.Town.Materials.Sum() == 3, "non-milestone merge gives none");
        game.State.TotalMerges = 3;
        town.OnMergeCompleted();
        Check(game.State.Town.Materials.Sum() == 4, "third merge gives material");

        var cost = TownCatalog.GetCost(0, 1);
        game.State.Coins = 2000;
        game.State.Town.Materials = new[] { 100, 100, 100, 100 };
        int packsBefore = game.State.Album.Packs;
        Check(town.CanBuild(0), "enough resources to build");
        Check(town.Construct(0), "construction succeeds");
        Check(town.Stage(0) == 1, "stage increased");
        Check(game.State.Coins == 2000 - cost.Coins, "coins charged once");
        Check(game.State.Town.Materials[0] == 100 - cost.Wood, "wood deducted");
        Check(game.State.Album.Packs == packsBefore + 1, "building card pack bonus");

        Check(town.Construct(0), "second stage");
        Check(town.Construct(0), "third stage");
        Check(town.Stage(0) == 3, "all three stages complete");
        Check(!town.Construct(0), "prevent upgrades beyond final stage");

        var saved = game.State;
        var restored = new GameData();
        Check(restored.Restore(saved), "restore town data");
        Check(restored.Town.Stage(0) == 3, "building stage is persisted");
        Check(restored.State.Town.Materials[0] == game.State.Town.Materials[0],
            "materials are persisted");

        Check(restored.Restore(new SaveData { Town = null! }), "old saves compatible");
        Check(restored.State.Town != null && restored.State.Town.Materials.Length == 4,
            "old save migrated to default town");
        Console.WriteLine("TOWN TESTS PASSED: buildings, coin costs, materials, packs, migration");
    }
}
