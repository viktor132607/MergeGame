using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

public sealed class TileData
{
    public int Family { get; set; }
    public int Tier { get; set; }
    public TileData Copy() => new() { Family = Family, Tier = Tier };
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
        State.Orders.Clear();
        for (int i = 0; i < 3; i++) State.Orders.Add(GenerateOrder());
        Message = "Start merging, or generate unlimited free items!";
    }

    public bool Restore(SaveData? saved)
    {
        if (saved == null || saved.Tiles == null || saved.Tiles.Length != FoodCatalog.CellCount ||
            saved.Orders == null || saved.Level < 1 ||
            saved.Tiles.Any(t => t != null && (t.Family < 0 || t.Family >= FoodCatalog.Families || t.Tier < 1 || t.Tier > FoodCatalog.MaxTier)) ||
            saved.Orders.Any(o => o.Family < 0 || o.Family >= FoodCatalog.Families ||
                                  o.Tier < 1 || o.Tier > FoodCatalog.MaxTier || o.Coins < 0 || o.Experience < 0))
            return false;
        State = saved;
        State.Coins = Math.Max(0, State.Coins);
        State.Experience = Math.Max(0, State.Experience);
        State.CompletedOrders = Math.Max(0, State.CompletedOrders);
        State.TotalMerges = Math.Max(0, State.TotalMerges);
        State.SelectedGenerator = Math.Clamp(State.SelectedGenerator, 0, CollectionCatalog.TotalCount - 1);
        State.Album ??= new CardAlbumState();
        CardAlbum.Normalize(State.Album);
        State.Town ??= new TownState();
        TownSystem.Normalize(State.Town);
        if (!CollectionCatalog.IsUnlocked(State.SelectedGenerator, Town)) State.SelectedGenerator = 0;
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

    public bool Spawn(int family)
    {
        if (!CollectionCatalog.IsValid(family)) return false;
        if (!CollectionCatalog.IsUnlocked(family, Town))
        {
            Message = "Construct " + TownCatalog.Buildings[CollectionCatalog.BuildingForFamily(family)].Name +
                      " to unlock this collection.";
            return false;
        }
        State.SelectedGenerator = family;
        int roll = _random.Next(100);
        int tier = roll < 72 ? 1 : roll < 96 ? 2 : 3;
        if (State.Level < 3 && tier == 3) tier = 2;
        if (!PlaceRandomly(new TileData { Family = family, Tier = tier }))
        {
            Message = "Board full. Serve an order or recycle an item to make room.";
            return false;
        }
        ClearHints();
        Message = "Free " + FoodCatalog.Name(family, tier) + " generated. Energy is unlimited!";
        return true;
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

        if (destination != null && source.Family == destination.Family &&
            source.Tier == destination.Tier && source.Tier < FoodCatalog.MaxTier)
        {
            State.Tiles[to] = new TileData { Family = source.Family, Tier = source.Tier + 1 };
            State.Tiles[from] = null;
            State.TotalMerges++;
            Message = "MERGED! Discovered " + FoodCatalog.Name(source.Family, source.Tier + 1) + ".";
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
        return State.Tiles.Any(t => t != null && t.Family == order.Family && t.Tier == order.Tier);
    }

    public bool Serve(int orderIndex)
    {
        if (orderIndex < 0 || orderIndex >= State.Orders.Count) return false;
        OrderData order = State.Orders[orderIndex];
        int index = Array.FindIndex(State.Tiles, t =>
            t != null && t.Family == order.Family && t.Tier == order.Tier);
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
            var existing = State.Tiles.Where(t => t != null && families.Contains(t.Family))
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
        State.Tiles[Selected] = null;
        Selected = -1;
        ClearHints();
        Message = "Item recycled for free. A square is now available.";
        return true;
    }

    public bool Organize()
    {
        TileData[] ordered = State.Tiles.Where(t => t != null)
            .Select(t => t!).OrderBy(t => t.Family).ThenBy(t => t.Tier).ToArray();
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
            if (one == null || one.Tier >= FoodCatalog.MaxTier) continue;
            for (int j = i + 1; j < FoodCatalog.CellCount; j++)
            {
                TileData? two = State.Tiles[j];
                if (two != null && one.Family == two.Family && one.Tier == two.Tier)
                {
                    HintOne = i;
                    HintTwo = j;
                    Message = "Hint: merge the two glowing " + FoodCatalog.Name(one.Family, one.Tier) + " tiles.";
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
