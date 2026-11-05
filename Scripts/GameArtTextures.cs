using Godot;
using MergeHarvest;
using System;
using System.Collections.Generic;

// All board, order and collection illustrations are actual imported textures,
// not procedurally drawn icons. The same cached assets are reused everywhere.
public static class GameArtTextures
{
    private static readonly Dictionary<string, Texture2D?> Cache = new();

    private static Texture2D? Load(string path)
    {
        if (!Cache.TryGetValue(path, out var texture))
        {
            texture = ResourceLoader.Load<Texture2D>(path);
            Cache[path] = texture;
        }
        return texture;
    }

    public static Texture2D? Item(int family, int tier)
    {
        if (family < 0 || family >= CollectionCatalog.TotalCount ||
            tier < 1 || tier > CollectionCatalog.ItemsPerCollection) return null;
        return Load($"res://Assets/Items/{family}_{tier}.svg");
    }

    public static Texture2D? Generator(int family)
    {
        if (family < 3) return Item(family, 10);
        int building = family - 3;
        if (building >= TownCatalog.BuildingCount) return null;
        return Load($"res://Assets/Buildings/{building}.svg");
    }

    public static Texture2D? Part(int tier)
    {
        string type = tier switch
        {
            1 => "wood",
            2 => "hardware",
            3 => "bricks",
            _ => "glass"
        };
        return Load($"res://Assets/Materials/{type}.svg");
    }

    public static Texture2D? Tile(MergeHarvest.TileData tile) => tile.Kind switch
    {
        TileKind.Generator => Generator(tile.Family),
        TileKind.GeneratorPart => Part(tile.Tier),
        _ => Item(tile.Family, tile.Tier)
    };

    public static bool CheckCatalog(out string error)
    {
        for (int family = 0; family < CollectionCatalog.TotalCount; family++)
        {
            for (int tier = 1; tier <= CollectionCatalog.ItemsPerCollection; tier++)
            {
                if (Item(family, tier) == null)
                {
                    error = "Missing item sprite " + family + ":" + tier;
                    return false;
                }
            }
            if (Generator(family) == null)
            {
                error = "Missing generator sprite for " + family;
                return false;
            }
        }
        for (int tier = 1; tier <= 4; tier++)
        {
            if (Part(tier) == null)
            {
                error = "Missing generator part sprite " + tier;
                return false;
            }
        }
        error = "";
        return true;
    }
}
