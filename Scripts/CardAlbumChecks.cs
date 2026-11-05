using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

// Runs in CI via MERGE_CARD_TEST=1. No user saves are modified.
public static class CardAlbumChecks
{
    private static void Require(bool condition, string explanation)
    {
        if (!condition) throw new InvalidOperationException("CARD ALBUM TEST FAILED: " + explanation);
    }

    public static void Run()
    {
        var game = new GameData();
        var album = game.Cards;
        Require(CardCatalog.TotalCards == 54, "catalog length");
        Require(CardCatalog.SetCount == 6, "album sets");
        Require(game.State.Album.Packs == 1, "starter pack");
        Require(album.OpenPack(false, out var first) && first.Count == 3,
            "regular pack opening");
        Require(game.State.Album.Packs == 0, "regular pack consumption");
        Require(game.State.Album.LifetimeCardsOpened == 3, "card reveal count");

        game.State.CompletedOrders = 2;
        album.OnOrderCompleted();
        Require(game.State.Album.Packs == 1, "reward for two served orders");
        game.State.TotalMerges = 8;
        album.OnMergeCompleted();
        Require(game.State.Album.Packs == 2, "merge reward");

        game.State.CompletedOrders = 10;
        album.OnOrderCompleted();
        Require(game.State.Album.PremiumPacks == 1, "ten-order golden reward");
        Require(album.OpenPack(true, out var golden) && golden.Count == 5,
            "golden pack draws five cards");

        game.State.Album.OwnedCardIds = Enumerable.Range(0, CardCatalog.TotalCards).ToList();
        int previousStars = game.State.Album.Stars;
        Require(album.OpenPack(false, out var duplicatePack), "open duplicate pack");
        Require(duplicatePack.All(c => !c.NewCard), "duplicate identification");
        Require(game.State.Album.Stars > previousStars, "duplicate conversion to stars");

        game.State.Album.Stars = 120;
        int earlierPacks = game.State.Album.Packs;
        Require(album.ExchangeStars(false), "stars buy regular pack");
        Require(game.State.Album.Packs == earlierPacks + 1, "pack reward for stars");
        Require(game.State.Album.Stars == 90, "star cost");
        Require(album.ExchangeStars(true), "stars buy golden pack");
        Require(game.State.Album.Stars == 0, "golden cost");
        Require(!album.ExchangeStars(true), "insufficient stars cannot go negative");

        int coinsBefore = game.State.Coins;
        for (int i = 0; i < CardCatalog.SetCount; i++)
        {
            Require(album.ClaimSet(i), "first set claim " + i);
            Require(!album.ClaimSet(i), "set reward cannot repeat " + i);
        }
        Require(game.State.Coins == coinsBefore + 6 * 250, "all set coin rewards");
        Require(album.ClaimGrandPrize(), "album grand prize");
        Require(!album.ClaimGrandPrize(), "grand reward cannot repeat");
        Require(game.State.Coins == coinsBefore + 6 * 250 + 2000, "grand prize coins");

        var backup = game.State;
        var restored = new GameData();
        Require(restored.Restore(backup), "restore a saved game with cards");
        Require(restored.State.Album.OwnedCardIds.Count == 54, "cards persist");
        Require(restored.State.Album.GrandPrizeClaimed, "reward claim persists");

        var oldSave = new SaveData { Album = null! };
        Require(restored.Restore(oldSave), "old saves migrate");
        Require(restored.State.Album != null, "old save gains album");
        Require(restored.State.Album.OwnedCardIds.Count == 0, "new album is empty");

        Console.WriteLine("CARD ALBUM TESTS PASSED: packs, rarities, duplicates, stars, rewards, migration");
    }
}
