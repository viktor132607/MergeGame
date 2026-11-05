using System;
using System.Linq;

namespace MergeHarvest;

public static class SpawnerChecks
{
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("SPAWNER TEST FAILED: " + description);
    }

    public static void Run()
    {
        var game = new GameData();
        Check(game.State.Tiles.Count(t => t != null && t.Kind == TileKind.Generator) == 3,
            "three initial on-board generators");
        Check(game.State.Tiles.Count(t => t != null && t.Kind == TileKind.Item) == 18,
            "eighteen initial food items");
        int orchard = Array.FindIndex(game.State.Tiles, t =>
            t != null && t.Kind == TileKind.Generator && t.Family == 0);
        Check(orchard >= 0, "find original orchard generator");
        int occupied = game.State.Tiles.Count(t => t != null);
        Check(game.TapTile(orchard), "click generator to produce item");
        Check(game.State.Tiles.Count(t => t != null) == occupied + 1,
            "production adds exactly one item");
        Check(game.State.Tiles[orchard]!.Kind == TileKind.Generator,
            "generator remains after production");

        // Confirm guaranteed component production at tap six, without timers or energy.
        int partsBefore = game.State.ComponentDrops;
        game.State.GeneratorTaps = 5;
        Check(game.TapGenerator(orchard), "sixth generator tap");
        Check(game.State.ComponentDrops == partsBefore + 1,
            "guaranteed component drop");
        Check(game.State.Tiles.Any(t => t != null && t.Kind == TileKind.GeneratorPart),
            "component appears on board");

        // A pair of equal spawners upgrades to level two.
        var tiles = game.State.Tiles;
        int[] free = Enumerable.Range(0, tiles.Length).Where(i => tiles[i] == null).Take(2).ToArray();
        Check(free.Length == 2, "free cells for spawner merge");
        tiles[free[0]] = new TileData { Family = 0, Tier = 1, Kind = TileKind.Generator };
        tiles[free[1]] = new TileData { Family = 0, Tier = 1, Kind = TileKind.Generator };
        Check(game.Interact(free[0], free[1]), "merge generators");
        Check(tiles[free[0]] == null && tiles[free[1]]!.Tier == 2 &&
            tiles[free[1]]!.Kind == TileKind.Generator, "upgraded generator survives");
        Check(game.TapGenerator(free[1]), "upgraded generator produces items");

        // Merge parts through tiers one/two/three into a new actual generator.
        for (int tier = 1; tier <= 3; tier++)
        {
            int[] cells = Enumerable.Range(0, tiles.Length)
                .Where(i => tiles[i] == null).Take(2).ToArray();
            Check(cells.Length == 2, "free cells for part merge tier " + tier);
            tiles[cells[0]] = new TileData { Family = 1, Tier = tier, Kind = TileKind.GeneratorPart };
            tiles[cells[1]] = new TileData { Family = 1, Tier = tier, Kind = TileKind.GeneratorPart };
            Check(game.Interact(cells[0], cells[1]), "merge generator parts at tier " + tier);
            Check(tiles[cells[0]] == null, "part consumed tier " + tier);
            Check(tiles[cells[1]]!.Kind ==
                (tier == 3 ? TileKind.Generator : TileKind.GeneratorPart),
                "assembly creates correct item tier " + tier);
            Check(tiles[cells[1]]!.Tier == (tier == 3 ? 1 : tier + 1),
                "assembly output level " + tier);
            tiles[cells[1]] = null;
        }

        // Generators and their parts cannot be fulfilled as food orders.
        game.State.Orders[0] = new OrderData { Family = 1, Tier = 1, Coins = 20, Experience = 10 };
        for (int i = 0; i < tiles.Length; i++)
            if (tiles[i] != null && tiles[i]!.Kind == TileKind.Item &&
                tiles[i]!.Family == 1 && tiles[i]!.Tier == 1)
                tiles[i] = null;
        Check(!game.CanServe(0), "food orders ignore bakery spawners");

        // Construction creates a new on-board generator for that family.
        game.State.Coins = 100000;
        game.State.Town.Materials = new[] { 10000, 10000, 10000, 10000 };
        Check(game.Town.Construct(0), "build beach cafe");
        Check(game.State.Tiles.Any(t => t != null &&
            t.Family == 3 && t.Kind == TileKind.Generator),
            "new building places its on-board spawner");

        // The last generator may not be thrown away.
        int cafeIndex = Array.FindIndex(tiles, t => t != null &&
            t.Family == 3 && t.Kind == TileKind.Generator);
        Check(cafeIndex >= 0, "cafe generator can be located");
        Check(game.TapTile(cafeIndex), "tap cafe generator");
        // No generator should be lost when a save is serialized and reloaded.
        var copy = new GameData();
        Check(copy.Restore(game.State), "restore upgraded generator save");
        Check(copy.State.Tiles[cafeIndex]!.Kind == TileKind.Generator, "generator persisted");
        Check(copy.State.GeneratorTaps == game.State.GeneratorTaps, "tap counter persisted");

        // A legacy save with no new Kind properties is treated as food and
        // automatically gains initial generators if room is available.
        var legacy = new SaveData();
        legacy.Tiles[0] = new TileData { Family = 0, Tier = 2 };
        Check(copy.Restore(legacy), "legacy save loads");
        Check(copy.State.Tiles[0]!.Kind == TileKind.Item, "old tiles remain food");
        Check(copy.State.Tiles.Count(t => t != null && t.Kind == TileKind.Generator) == 3,
            "legacy save obtains missing starter generators");

        // No free board squares: tapping must not change or destroy anything.
        var blocked = new GameData();
        int seedGenerator = Array.FindIndex(blocked.State.Tiles,
            t => t != null && t.Kind == TileKind.Generator);
        for (int i = 0; i < blocked.State.Tiles.Length; i++)
            blocked.State.Tiles[i] ??= new TileData { Family = 0, Tier = 1 };
        int beforeTaps = blocked.State.GeneratorTaps;
        Check(!blocked.TapGenerator(seedGenerator), "full board blocks production");
        Check(blocked.State.GeneratorTaps == beforeTaps, "failed tap does not count");

        Console.WriteLine("SPAWNER TESTS PASSED: board taps, components, generator upgrades, town unlocks, saves");
    }
}
