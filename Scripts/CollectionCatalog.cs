using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeHarvest;

// IDs 0-2 belong to the original three food chains.
// Each completed first-stage town building unlocks one more unique 10-item chain.
public static class CollectionCatalog
{
    public const int BaseCount = 3;
    public const int BuildingCount = 20;
    public const int TotalCount = BaseCount + BuildingCount;
    public const int ItemsPerCollection = 10;
    public const int TotalItems = TotalCount * ItemsPerCollection;

    public static readonly string[] FamilyNames =
    {
        "Orchard", "Bakery", "Drinks",
        "Coffee House", "Flower Boutique", "Bread Studio", "Hotel Comfort",
        "Sailing Gear", "Lighthouse", "Island Cuisine", "Tropical Harvest",
        "Ice Cream", "Island Souvenirs", "Coastal Cabins", "Fishing",
        "Botanical Garden", "Music Festival", "Surf School", "Wellness Spa",
        "Sea Creatures", "Adventure Park", "Island History", "Royal Resort"
    };

    public static readonly string[] GeneratorNames =
    {
        "Orchard Basket", "Bakery Oven", "Drink Station",
        "Coffee Machine", "Flower Stand", "Bread Oven", "Hotel Trolley",
        "Marina Dock", "Signal Workshop", "Restaurant Kitchen", "Fruit Crate",
        "Frozen Counter", "Gift Workshop", "Cabin Supply", "Fishing Boat",
        "Greenhouse", "Music Stage", "Surf Workshop", "Spa Counter",
        "Aquarium Tank", "Adventure Camp", "Museum Archive", "Resort Lounge"
    };

    // Precisely ten stages per family, from an everyday object to a rare prestige item.
    public static readonly string[][] Names =
    {
        new[] { "Seed", "Sprout", "Berry", "Apple", "Lemon", "Grapes", "Fruit Bowl", "Smoothie", "Jam Set", "Feast" },
        new[] { "Flour", "Dough", "Cookie", "Croissant", "Cupcake", "Tart", "Pie", "Cake", "Dessert Set", "Banquet" },
        new[] { "Water", "Tea", "Juice", "Milkshake", "Coffee", "Iced Latte", "Punch", "Soda", "Fancy Cup", "Golden Drink" },
        new[] { "Coffee Bean", "Ground Coffee", "Espresso Shot", "Cappuccino", "Latte", "Mocha", "Affogato", "Cold Brew", "Coffee Flight", "Barista Crown" },
        new[] { "Flower Seed", "Daisy", "Tulip", "Sunflower", "Orchid", "Rose", "Bouquet", "Flower Basket", "Floral Arch", "Royal Garden" },
        new[] { "Wheat Ear", "Wheat Flour", "Starter Dough", "Bread Roll", "Baguette", "Sourdough", "Focaccia", "Artisan Basket", "Bread Sculpture", "Master Loaf" },
        new[] { "Cotton Cloth", "Clean Towel", "Pillow", "Soft Blanket", "Bed Linen", "Luxury Robe", "Suite Decor", "Room Service", "Penthouse Key", "Royal Suite" },
        new[] { "Rope Coil", "Sail Cloth", "Oar", "Life Buoy", "Compass", "Anchor", "Sailboat", "Yacht", "Cruise Ship", "Royal Fleet" },
        new[] { "Glass Shard", "Lamp Wick", "Oil Lamp", "Beacon Bulb", "Signal Lens", "Lantern", "Light Beam", "Beacon Tower", "Seafarer Medal", "Eternal Light" },
        new[] { "Sea Salt", "Herb Mix", "Fresh Salad", "Fish Taco", "Seafood Soup", "Paella", "Grilled Platter", "Chef Special", "Island Buffet", "Royal Dinner" },
        new[] { "Plant Pit", "Mango", "Papaya", "Pineapple", "Coconut", "Fruit Basket", "Tropical Juice", "Harvest Crate", "Orchard Cart", "Golden Harvest" },
        new[] { "Ice Cube", "Milk Scoop", "Vanilla Scoop", "Berry Scoop", "Waffle Cone", "Double Sundae", "Banana Split", "Ice Cream Cake", "Frozen Tower", "Rainbow Palace" },
        new[] { "Seashell", "Driftwood", "Postcard", "Keyring", "Shell Necklace", "Painted Mug", "Carved Figurine", "Gift Basket", "Collector Chest", "Island Heirloom" },
        new[] { "Wooden Peg", "Timber Beam", "Door Frame", "Window", "Wooden Chair", "Cozy Hammock", "Porch Swing", "Cabin Porch", "Luxury Bungalow", "Seaside Villa" },
        new[] { "Fishing Hook", "Fishing Line", "Bobber", "Fishing Rod", "Net", "Crab Pot", "Fresh Catch", "Fish Basket", "Deep Sea Trophy", "Captain Catch" },
        new[] { "Soil Pot", "Little Fern", "Potted Herb", "Cactus", "Bonsai", "Water Lily", "Garden Bed", "Rare Orchid", "Greenhouse", "Tree of Life" },
        new[] { "Music Note", "Drumstick", "Tambourine", "Flute", "Guitar", "Ukulele", "Violin", "Saxophone", "Grand Piano", "Golden Concert" },
        new[] { "Wax Block", "Surf Wax", "Fin", "Beach Board", "Shortboard", "Longboard", "Wave Rider", "Pro Surfboard", "Surf Trophy", "Legendary Wave" },
        new[] { "Soap Bar", "Bath Salt", "Aroma Oil", "Face Mask", "Scented Candle", "Bath Set", "Massage Kit", "Spa Basket", "Wellness Fountain", "Royal Spa" },
        new[] { "Sea Pebble", "Coral", "Sea Urchin", "Starfish", "Seahorse", "Clownfish", "Sea Turtle", "Dolphin", "Manta Ray", "Ocean Kingdom" },
        new[] { "Map Scrap", "Explorer Map", "Water Bottle", "Hiking Boots", "Rope Ladder", "Treehouse", "Zipline", "Treasure Hunt", "Adventure Trophy", "Explorer Legend" },
        new[] { "Old Coin", "Pottery Shard", "Ancient Key", "Fossil", "Clay Vase", "Carved Tablet", "Pirate Compass", "Ancient Statue", "Royal Relic", "Lost Civilization" },
        new[] { "Pearl", "Silk Ribbon", "Gold Bracelet", "Diamond Ring", "Luxury Perfume", "Champagne Set", "Golden Crown", "Royal Gift", "Resort Gala", "Grand Paradise" }
    };

    public static bool IsValid(int family) => family >= 0 && family < TotalCount;
    public static bool IsBuildingCollection(int family) => family >= BaseCount && family < TotalCount;
    public static int BuildingForFamily(int family) =>
        IsBuildingCollection(family) ? family - BaseCount : -1;
    public static int FamilyForBuilding(int buildingId) =>
        buildingId >= 0 && buildingId < BuildingCount ? buildingId + BaseCount : -1;
    public static string ItemName(int family, int tier) =>
        IsValid(family) && tier >= 1 && tier <= ItemsPerCollection ?
            Names[family][tier - 1] : "Unknown";

    public static bool IsUnlocked(int family, TownSystem town) =>
        family < BaseCount && family >= 0 ||
        IsBuildingCollection(family) && town.Stage(BuildingForFamily(family)) > 0;

    public static IEnumerable<int> UnlockedFamilies(TownSystem town) =>
        Enumerable.Range(0, TotalCount).Where(f => IsUnlocked(f, town));
}
