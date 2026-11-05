using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

public enum TileKind { Item = 0, Generator = 1, GeneratorPart = 2 }

public sealed class TileData
{
    public int Family { get; set; }
    public int Tier { get; set; }
    // Missing Kind in old save files defaults to Item, keeping old progress intact.
    public TileKind Kind { get; set; } = TileKind.Item;
    public TileData Copy() => new() { Family = Family, Tier = Tier, Kind = Kind };
}

public sealed class OrderData
{
    public int Family { get; set; }
    public int Tier { get; set; }
    public int Coins { get; set; }
    public int Experience { get; set; }
}

public sealed class SaveData
{
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int Coins { get; set; } = 100;
    public int CompletedOrders { get; set; }
    public int TotalMerges { get; set; }
    public int SelectedGenerator { get; set; }
    public int GeneratorTaps { get; set; }
    public int ComponentDrops { get; set; }
    public CardAlbumState Album { get; set; } = new();
    public TownState Town { get; set; } = new();
    public TileData?[] Tiles { get; set; } = new TileData?[FoodCatalog.CellCount];
    public List<OrderData> Orders { get; set; } = new();
}

public static class FoodCatalog
{
    public const int Columns = 7;
    public const int Rows = 7;
    public const int CellCount = Columns * Rows;
    public const int Families = CollectionCatalog.TotalCount;
    public const int MaxTier = CollectionCatalog.ItemsPerCollection;

    public static readonly string[] FamilyNames = CollectionCatalog.FamilyNames;
    public static readonly string[] GeneratorNames = CollectionCatalog.GeneratorNames;
    public static readonly string[][] Names = CollectionCatalog.Names;

    public static string Name(int family, int tier) => CollectionCatalog.ItemName(family, tier);
}

public sealed class GameData
{
    private readonly Random _random = new();
    public CardAlbum Cards { get; }
    public TownSystem Town { get; }
    public SaveData State { get; private set; } = new();
    public int Selected { get; private set; } = -1;
    public int HintOne { get; private set; } = -1;
    public int HintTwo { get; private set; } = -1;
    public string Message { get; private set; } = "Merge two matching items to discover something new.";
    public int ExperienceRequired => 65 + State.Level * 35;
    public const int MaxGeneratorTier = 5;
    public const int MaxComponentTier = 4;
    public int LastProducedIndex { get; private set; } = -1;

    public GameData()
    {
        Cards = new CardAlbum(this);
        Town = new TownSystem(this);
        NewGame();
    }

    public void AppendMessage(string extra) => Message += extra;
    public void SetMessage(string message) => Message = message;

    public void GrantAlbumReward(int coins, int xp)
    {
        State.Coins += coins;
        State.Experience += xp;
        while (State.Experience >= ExperienceRequired)
        {
            State.Experience -= ExperienceRequired;
            State.Level++;
        }
    }

    public void NewGame()
    {
        State = new SaveData();
        Selected = -1;
        ClearHints();
        for (int family = 0; family < CollectionCatalog.BaseCount; family++)
        {
            for (int i = 0; i < 4; i++) PlaceRandomly(new TileData { Family = family, Tier = 1 });
            for (int i = 0; i < 2; i++) PlaceRandomly(new TileData { Family = family, Tier = 2 });
        }
        // Three starter generators live on the board: production happens by clicking them.
        for (int family = 0; family < CollectionCatalog.BaseCount; family++)
            PlaceRandomly(new TileData { Family = family, Tier = 1, Kind = TileKind.Generator });
        State.Orders.Clear();
        for (int i = 0; i < 3; i++) State.Orders.Add(GenerateOrder());
        Message = "Start merging, or generate unlimited free items!";
    }

    public bool Restore(SaveData? saved)
    {
        if (saved == null || saved.Tiles == null || saved.Tiles.Length != FoodCatalog.CellCount ||
            saved.Orders == null || saved.Level < 1 ||
            saved.Tiles.Any(t => t != null && (t.Family < 0 || t.Family >= FoodCatalog.Families ||
                !Enum.IsDefined(t.Kind) || t.Tier < 1 ||
                t.Tier > (t.Kind == TileKind.Item ? FoodCatalog.MaxTier :
                          t.Kind == TileKind.Generator ? MaxGeneratorTier : MaxComponentTier))) ||
            saved.Orders.Any(o => o.Family < 0 || o.Family >= FoodCatalog.Families ||
                                  o.Tier < 1 || o.Tier > FoodCatalog.MaxTier || o.Coins < 0 || o.Experience < 0))
            return false;
        State = saved;
        State.Coins = Math.Max(0, State.Coins);
        State.Experience = Math.Max(0, State.Experience);
        State.CompletedOrders = Math.Max(0, State.CompletedOrders);
        State.TotalMerges = Math.Max(0, State.TotalMerges);
        State.GeneratorTaps = Math.Max(0, State.GeneratorTaps);
        State.ComponentDrops = Math.Max(0, State.ComponentDrops);
        State.SelectedGenerator = Math.Clamp(State.SelectedGenerator, 0, CollectionCatalog.TotalCount - 1);
        State.Album ??= new CardAlbumState();
        CardAlbum.Normalize(State.Album);
        State.Town ??= new TownState();
        TownSystem.Normalize(State.Town);
        if (!CollectionCatalog.IsUnlocked(State.SelectedGenerator, Town)) State.SelectedGenerator = 0;
        // Old saves were created before on-board generators existed.
        // Add a starter generator to a free space, never overwrite existing items.
        for (int family = 0; family < CollectionCatalog.TotalCount; family++)
            if (CollectionCatalog.IsUnlocked(family, Town) && !HasGeneratorOrPart(family))
                PlaceRandomly(new TileData { Family = family, Tier = 1, Kind = TileKind.Generator });
        while (State.Orders.Count < 3) State.Orders.Add(GenerateOrder());
        if (State.Orders.Count > 3) State.Orders = State.Orders.Take(3).ToList();
        Selected = -1;
        ClearHints();
        Message = "Welcome back! Your board was restored.";
        return true;
    }

    private bool PlaceRandomly(TileData item)
    {
        int[] free = Enumerable.Range(0, FoodCatalog.CellCount)
            .Where(i => State.Tiles[i] == null).ToArray();
        if (free.Length == 0) return false;
        State.Tiles[free[_random.Next(free.Length)]] = item;
        return true;
    }

    public bool HasGeneratorOrPart(int family) =>
        State.Tiles.Any(t => t != null && t.Family == family &&
            (t.Kind == TileKind.Generator || t.Kind == TileKind.GeneratorPart));

    public bool PlaceUnlockedGenerator(int family)
    {
        if (!CollectionCatalog.IsUnlocked(family, Town)) return false;
        // Do not duplicate an existing generator or component chain for free.
        if (HasGeneratorOrPart(family))
        {
            Message = "This generator is already on the board. Merge its components or tap it.";
            return false;
        }
        if (!PlaceRandomly(new TileData { Family = family, Tier = 1, Kind = TileKind.Generator }))
        {
            Message = "Board full! Make space to place the new generator.";
            return false;
        }
        State.SelectedGenerator = family;
        Selected = -1;
        ClearHints();
        Message = "Placed " + CollectionCatalog.GeneratorNames[family] +
            " on the board. Click it for free items and generator parts!";
        return true;
    }

    // Compatibility entry point: sidebar clicks focus an on-board generator, not conjure an item.
    public bool Spawn(int family)
    {
        if (!CollectionCatalog.IsUnlocked(family, Town)) return false;
        State.SelectedGenerator = family;
        int generatorIndex = Array.FindIndex(State.Tiles, t =>
            t != null && t.Family == family && t.Kind == TileKind.Generator);
        if (generatorIndex < 0) return PlaceUnlockedGenerator(family);
        return TapGenerator(generatorIndex);
    }

    private int FindEmptyOutputCell(int source)
    {
        int col = source % FoodCatalog.Columns;
        int row = source / FoodCatalog.Columns;
        // Favor adjacent tiles; fall back to another free board square so the
        // generator stays usable even when boxed in.
        foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (-1, 0), (0, -1),
                                        (1, 1), (-1, 1), (1, -1), (-1, -1) })
        {
            int x = col + dx;
            int y = row + dy;
            if (x >= 0 && x < FoodCatalog.Columns && y >= 0 && y < FoodCatalog.Rows)
            {
                int destination = y * FoodCatalog.Columns + x;
                if (State.Tiles[destination] == null) return destination;
            }
        }
        return Array.FindIndex(State.Tiles, t => t == null);
    }

    public bool TapGenerator(int index)
    {
        if (index < 0 || index >= State.Tiles.Length) return false;
        TileData? generator = State.Tiles[index];
        if (generator == null || generator.Kind != TileKind.Generator ||
            !CollectionCatalog.IsUnlocked(generator.Family, Town)) return false;
        int destination = FindEmptyOutputCell(index);
        if (destination < 0)
        {
            Message = "Board full — merge or serve items to free a square.";
            return false;
        }
        State.GeneratorTaps++;
        // Every 6th tap guarantees a component, plus a 12% chance on other taps.
        bool component = State.GeneratorTaps % 6 == 0 || _random.Next(100) < 12;
        // Higher generator tiers can drop higher-tier item chains and components.
        int tier = component ? (_random.Next(100) < 10 + generator.Tier * 8 ? 2 : 1) :
            (_random.Next(100) < generator.Tier * 14 ? 2 : 1);
        State.Tiles[destination] = new TileData
        {
            Family = generator.Family,
            Tier = tier,
            Kind = component ? TileKind.GeneratorPart : TileKind.Item
        };
        if (component) State.ComponentDrops++;
        State.SelectedGenerator = generator.Family;
        Selected = -1;
        LastProducedIndex = destination;
        ClearHints();
        Message = component
            ? "FREE GENERATOR PART! Merge matching components to build a stronger spawner."
            : CollectionCatalog.ItemName(generator.Family, tier) + " produced — unlimited energy!";
        return true;
    }

    public bool TapTile(int index)
    {
        if (index < 0 || index >= State.Tiles.Length) return false;
        TileData? tile = State.Tiles[index];
        if (tile != null && tile.Kind == TileKind.Generator)
            return TapGenerator(index);
        return Interact(index, index);
    }

    public bool Interact(int pressedIndex, int releasedIndex)
    {
        if (pressedIndex < 0 || pressedIndex >= FoodCatalog.CellCount) return false;
        if (releasedIndex < 0 || releasedIndex >= FoodCatalog.CellCount) return false;

        if (pressedIndex != releasedIndex && State.Tiles[pressedIndex] != null)
            return MoveOrMerge(pressedIndex, releasedIndex);

        if (Selected == -1)
        {
            if (State.Tiles[releasedIndex] == null) return false;
            Selected = releasedIndex;
            ClearHints();
            Message = FoodCatalog.Name(State.Tiles[releasedIndex]!.Family, State.Tiles[releasedIndex]!.Tier) +
                      " selected. Move or merge it.";
            return true;
        }
        if (Selected == releasedIndex)
        {
            Selected = -1;
            Message = "Selection cleared.";
            return true;
        }
        if (State.Tiles[releasedIndex] == null && State.Tiles[Selected] == null)
        {
            Selected = -1;
            return false;
        }
        return MoveOrMerge(Selected, releasedIndex);
    }

    private bool MoveOrMerge(int from, int to)
    {
        if (from == to || State.Tiles[from] == null) return false;
        TileData source = State.Tiles[from]!;
        TileData? destination = State.Tiles[to];

        int maxTier = source.Kind switch
        {
            TileKind.Generator => MaxGeneratorTier,
            TileKind.GeneratorPart => MaxComponentTier,
            _ => FoodCatalog.MaxTier
        };
        if (destination != null && source.Kind == destination.Kind &&
            source.Family == destination.Family &&
            source.Tier == destination.Tier && source.Tier < maxTier)
        {
            TileKind resultKind = source.Kind;
            int resultTier = source.Tier + 1;
            if (source.Kind == TileKind.GeneratorPart && resultTier == MaxComponentTier)
            {
                // Two nearly complete generator mechanisms assemble a real level-1 spawner.
                resultKind = TileKind.Generator;
                resultTier = 1;
            }
            State.Tiles[to] = new TileData { Family = source.Family, Tier = resultTier, Kind = resultKind };
            State.Tiles[from] = null;
            State.TotalMerges++;
            Message = resultKind == TileKind.Generator
                ? "SPAWNER UPGRADED! " + CollectionCatalog.GeneratorNames[source.Family] +
                  " level " + resultTier + "."
                : source.Kind == TileKind.GeneratorPart
                    ? "GENERATOR COMPONENT merged to level " + resultTier + "."
                    : "MERGED! Discovered " + FoodCatalog.Name(source.Family, resultTier) + ".";
            Cards.OnMergeCompleted();
            Town.OnMergeCompleted();
        }
        else
        {
            State.Tiles[to] = source;
            State.Tiles[from] = destination;
            Message = destination == null ? "Item moved." : "Items swapped.";
        }
        Selected = -1;
        ClearHints();
        return true;
    }

    public bool CanServe(int orderIndex)
    {
        if (orderIndex < 0 || orderIndex >= State.Orders.Count) return false;
        OrderData order = State.Orders[orderIndex];
        return State.Tiles.Any(t => t != null && t.Kind == TileKind.Item &&
            t.Family == order.Family && t.Tier == order.Tier);
    }

    public bool Serve(int orderIndex)
    {
        if (orderIndex < 0 || orderIndex >= State.Orders.Count) return false;
        OrderData order = State.Orders[orderIndex];
        int index = Array.FindIndex(State.Tiles, t =>
            t != null && t.Kind == TileKind.Item &&
            t.Family == order.Family && t.Tier == order.Tier);
        if (index < 0)
        {
            Message = "Merge items to make " + FoodCatalog.Name(order.Family, order.Tier) + " first.";
            return false;
        }
        State.Tiles[index] = null;
        State.Coins += order.Coins;
        State.Experience += order.Experience;
        State.CompletedOrders++;
        int levels = 0;
        while (State.Experience >= ExperienceRequired)
        {
            State.Experience -= ExperienceRequired;
            State.Level++;
            levels++;
        }
        State.Orders[orderIndex] = GenerateOrder();
        Selected = -1;
        ClearHints();
        Message = levels > 0
            ? "ORDER COMPLETE! +" + order.Coins + " coins. Level " + State.Level + " reached!"
            : "ORDER COMPLETE! +" + order.Coins + " coins and +" + order.Experience + " XP.";
        Cards.OnOrderCompleted();
        Town.OnOrderCompleted();
        return true;
    }

    private OrderData GenerateOrder()
    {
        // Orders only request collections whose generator the player owns.
        // Favor the actively selected collection and items already present on the board.
        int[] families = CollectionCatalog.UnlockedFamilies(Town).ToArray();
        int family;
        int roll = _random.Next(100);
        if (roll < 45 && families.Contains(State.SelectedGenerator))
            family = State.SelectedGenerator;
        else if (roll < 75)
        {
            var existing = State.Tiles.Where(t => t != null && t.Kind == TileKind.Item &&
                families.Contains(t.Family))
                .Select(t => t!.Family).Distinct().ToArray();
            family = existing.Length > 0 ? existing[_random.Next(existing.Length)] :
                families[_random.Next(families.Length)];
        }
        else
            family = families[_random.Next(families.Length)];
        int maxTier = Math.Min(5, 3 + (State.Level - 1) / 4);
        int tier = _random.Next(2, maxTier + 1);
        return new OrderData
        {
            Family = family,
            Tier = tier,
            Coins = 8 + tier * tier * 5,
            Experience = 10 + tier * 12
        };
    }

    public bool SelectGenerator(int family)
    {
        if (!CollectionCatalog.IsUnlocked(family, Town)) return false;
        State.SelectedGenerator = family;
        Message = CollectionCatalog.GeneratorNames[family] + " ready — unlimited items.";
        return true;
    }

    public bool Recycle()
    {
        if (Selected < 0 || State.Tiles[Selected] == null)
        {
            Message = "Select a tile to recycle it for free.";
            return false;
        }
        // Never recycle the last generator/component of an unlocked family.
        var tile = State.Tiles[Selected]!;
        if (tile.Kind != TileKind.Item && State.Tiles.Count(t =>
            t != null && t.Family == tile.Family && t.Kind != TileKind.Item) <= 1)
        {
            Message = "Keep your last generator! It is needed to make this collection.";
            return false;
        }
        State.Tiles[Selected] = null;
        Selected = -1;
        ClearHints();
        Message = "Item recycled for free. A square is now available.";
        return true;
    }

    public bool Organize()
    {
        TileData[] ordered = State.Tiles.Where(t => t != null)
            .Select(t => t!).OrderBy(t => t.Kind).ThenBy(t => t.Family)
            .ThenBy(t => t.Tier).ToArray();
        State.Tiles = new TileData?[FoodCatalog.CellCount];
        for (int i = 0; i < ordered.Length; i++) State.Tiles[i] = ordered[i];
        Selected = -1;
        ClearHints();
        Message = "Items organized by family and tier.";
        return true;
    }

    public bool ShowHint()
    {
        ClearHints();
        for (int i = 0; i < FoodCatalog.CellCount; i++)
        {
            TileData? one = State.Tiles[i];
            if (one == null || one.Tier >= (one.Kind == TileKind.Generator ?
                MaxGeneratorTier : one.Kind == TileKind.GeneratorPart ?
                MaxComponentTier : FoodCatalog.MaxTier)) continue;
            for (int j = i + 1; j < FoodCatalog.CellCount; j++)
            {
                TileData? two = State.Tiles[j];
                if (two != null && one.Kind == two.Kind &&
                    one.Family == two.Family && one.Tier == two.Tier)
                {
                    HintOne = i;
                    HintTwo = j;
                    Message = "Hint: merge the two glowing " +
                        (one.Kind == TileKind.Generator ? "spawners" :
                        one.Kind == TileKind.GeneratorPart ? "generator components" :
                        FoodCatalog.Name(one.Family, one.Tier)) + ".";
                    return true;
                }
            }
        }
        Message = "No matching pairs. Generate more free items.";
        return false;
    }

    public void ClearSelection()
    {
        Selected = -1;
        ClearHints();
        Message = "Selection cleared.";
    }

    private void ClearHints()
    {
        HintOne = -1;
        HintTwo = -1;
    }
}
