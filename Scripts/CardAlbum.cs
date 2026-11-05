using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

public sealed class CardAlbumState
{
    public List<int> OwnedCardIds { get; set; } = new();
    public List<int> ClaimedSetIds { get; set; } = new();
    public bool GrandPrizeClaimed { get; set; }
    public int Packs { get; set; } = 1;
    public int PremiumPacks { get; set; }
    public int Stars { get; set; }
    public int LifetimeCardsOpened { get; set; }
}

public readonly record struct CardDefinition(int Id, int SetIndex, string Name, int Rarity)
{
    public string StarsText => new string('★', Rarity);
}

public readonly record struct CardReveal(CardDefinition Card, bool NewCard);

public static class CardCatalog
{
    public const int CardsPerSet = 9;
    public const int SetCount = 6;
    public const int TotalCards = CardsPerSet * SetCount;

    public static readonly string[] SetNames =
    {
        "Tropical Escape", "Sweet Bakery", "Ocean Treasures",
        "Garden Picnic", "Golden Kitchen", "Dreamy Desserts"
    };

    private static readonly string[][] CardNames =
    {
        new[] { "Seaside Sunrise", "Tropical Juice", "Palm Path", "Sun Hat", "Beach Picnic", "Island Boat", "Lagoon", "Starfish Cove", "Golden Sunset" },
        new[] { "Flour Magic", "Little Cookie", "Warm Croissant", "Berry Muffin", "Sugar Cloud", "Sweet Tart", "Cupcake Tower", "Golden Pie", "Master Baker" },
        new[] { "Blue Waves", "Coral Reef", "Sea Turtle", "Little Dolphin", "Treasure Map", "Ocean Shell", "Jellyfish", "Pearl Chest", "Ocean Palace" },
        new[] { "Fresh Berries", "Flower Basket", "Lemonade", "Apple Picnic", "Butterfly", "Garden Bench", "Spring Bouquet", "Secret Meadow", "Picnic Queen" },
        new[] { "Copper Pot", "Herb Jar", "Baking Oven", "Wooden Spoon", "Chef Apron", "Citrus Plate", "Golden Teapot", "Chef Trophy", "Royal Banquet" },
        new[] { "Cream Puff", "Cherry Swirl", "Chocolate Kiss", "Peach Cake", "Macarons", "Ice Cream", "Rainbow Candy", "Dream Cake", "Sugar Castle" }
    };

    // Exactly one 5-star and one 4-star card per set.
    private static readonly int[] Rarities = { 1, 1, 2, 2, 2, 3, 3, 4, 5 };

    public static CardDefinition Get(int id)
    {
        if (id < 0 || id >= TotalCards) throw new ArgumentOutOfRangeException(nameof(id));
        int set = id / CardsPerSet;
        int number = id % CardsPerSet;
        return new CardDefinition(id, set, CardNames[set][number], Rarities[number]);
    }

    public static int CountOwned(CardAlbumState state, int setIndex) =>
        setIndex < 0 || setIndex >= SetCount ? 0 :
        state.OwnedCardIds.Count(id => id / CardsPerSet == setIndex);

    public static bool IsComplete(CardAlbumState state, int setIndex) =>
        CountOwned(state, setIndex) == CardsPerSet;

    public static bool IsAlbumComplete(CardAlbumState state) =>
        state.OwnedCardIds.Count == TotalCards;
}

public sealed class CardAlbum
{
    private readonly Random _random = new();
    private readonly GameData _game;

    public CardAlbum(GameData game) => _game = game;
    public CardAlbumState State => _game.State.Album;

    public int Collected => State.OwnedCardIds.Count;
    public int CompleteSets => Enumerable.Range(0, CardCatalog.SetCount)
        .Count(i => CardCatalog.IsComplete(State, i));

    public void OnOrderCompleted()
    {
        // One pack every two served orders, forever.
        if (_game.State.CompletedOrders % 2 == 0)
        {
            State.Packs++;
            _game.AppendMessage(" +1 card pack earned!");
        }
        if (_game.State.CompletedOrders % 10 == 0)
        {
            State.PremiumPacks++;
            _game.AppendMessage(" +1 golden card pack!");
        }
    }

    public void OnMergeCompleted()
    {
        // A pack every 8 successful merges.
        if (_game.State.TotalMerges % 8 == 0)
        {
            State.Packs++;
            _game.AppendMessage(" +1 card pack from merging!");
        }
    }

    public bool OpenPack(bool golden, out List<CardReveal> revealed)
    {
        revealed = new List<CardReveal>();
        if (golden ? State.PremiumPacks <= 0 : State.Packs <= 0) return false;

        if (golden) State.PremiumPacks--;
        else State.Packs--;

        int count = golden ? 5 : 3;
        for (int i = 0; i < count; i++)
        {
            // Open cards one at a time so players can unlock whole sets from a pack.
            CardDefinition card = DrawCard(golden);
            bool isNew = !State.OwnedCardIds.Contains(card.Id);
            if (isNew) State.OwnedCardIds.Add(card.Id);
            else State.Stars += card.Rarity;
            revealed.Add(new CardReveal(card, isNew));
            State.LifetimeCardsOpened++;
        }
        State.OwnedCardIds.Sort();
        _game.SetMessage("Opened " + (golden ? "golden" : "regular") +
                         " pack: " + revealed.Count(r => r.NewCard) + " new cards, +" +
                         revealed.Where(r => !r.NewCard).Sum(r => r.Card.Rarity) + " stars.");
        return true;
    }

    private CardDefinition DrawCard(bool golden)
    {
        // Rarity weighting: golden packs favor 3–5 stars.
        int[] weights = golden ? new[] { 3, 7, 11, 12, 8 } : new[] { 40, 28, 18, 10, 4 };
        int total = 0;
        for (int i = 0; i < CardCatalog.TotalCards; i++)
            total += weights[CardCatalog.Get(i).Rarity - 1];

        int roll = _random.Next(total);
        for (int i = 0; i < CardCatalog.TotalCards; i++)
        {
            int weight = weights[CardCatalog.Get(i).Rarity - 1];
            if (roll < weight) return CardCatalog.Get(i);
            roll -= weight;
        }
        return CardCatalog.Get(0);
    }

    public bool ExchangeStars(bool golden)
    {
        int cost = golden ? 90 : 30;
        if (State.Stars < cost)
        {
            _game.SetMessage("Not enough duplicate stars: need " + cost + ".");
            return false;
        }
        State.Stars -= cost;
        if (golden) State.PremiumPacks++;
        else State.Packs++;
        _game.SetMessage("Exchanged " + cost + " duplicate stars for a " +
                         (golden ? "golden" : "regular") + " card pack.");
        return true;
    }

    public bool ClaimSet(int index)
    {
        if (index < 0 || index >= CardCatalog.SetCount ||
            !CardCatalog.IsComplete(State, index) || State.ClaimedSetIds.Contains(index))
            return false;

        State.ClaimedSetIds.Add(index);
        _game.GrantAlbumReward(250, 100);
        State.Packs += 2;
        _game.SetMessage("SET COMPLETE! " + CardCatalog.SetNames[index] +
                         " — 250 coins, 100 XP, 2 free packs!");
        return true;
    }

    public bool ClaimGrandPrize()
    {
        if (!CardCatalog.IsAlbumComplete(State) || State.GrandPrizeClaimed) return false;
        // Grand prize requires claiming all six sets first.
        if (State.ClaimedSetIds.Distinct().Count() != CardCatalog.SetCount) return false;

        State.GrandPrizeClaimed = true;
        _game.GrantAlbumReward(2000, 1000);
        State.PremiumPacks += 5;
        _game.SetMessage("ALBUM COMPLETE! +2000 coins, +1000 XP, +5 golden packs!");
        return true;
    }

    public static void Normalize(CardAlbumState state)
    {
        state.Packs = Math.Clamp(state.Packs, 0, 1000000);
        state.PremiumPacks = Math.Clamp(state.PremiumPacks, 0, 1000000);
        state.Stars = Math.Clamp(state.Stars, 0, 1000000);
        state.LifetimeCardsOpened = Math.Max(0, state.LifetimeCardsOpened);
        state.OwnedCardIds = (state.OwnedCardIds ?? new List<int>())
            .Where(id => id >= 0 && id < CardCatalog.TotalCards).Distinct().OrderBy(id => id).ToList();
        state.ClaimedSetIds = (state.ClaimedSetIds ?? new List<int>())
            .Where(id => id >= 0 && id < CardCatalog.SetCount && CardCatalog.IsComplete(state, id))
            .Distinct().OrderBy(id => id).ToList();
        if (!CardCatalog.IsAlbumComplete(state) ||
            state.ClaimedSetIds.Count < CardCatalog.SetCount)
            state.GrandPrizeClaimed = false;
    }
}
