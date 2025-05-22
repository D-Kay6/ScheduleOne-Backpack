using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI.Shop;
using UnityEngine;

namespace Backpack;

public class BackpackListing : ShopListing
{
    public BackpackListing(Backpack backpack)
    {
        name = backpack.Name;
        Item = backpack.ItemDefinition;
        LimitedStock = true;
        DefaultStock = 1;
        CanBeDelivered = false;
        CurrentStock = 1;
    }
}

public class Backpack
{
    public string Name { get; set; } = "Default Backpack";
    public string Description { get; set; } = "This is a default backpack.";

    public int Rows { get; set; } = 2;
    public int Columns { get; set; } = 2;

    public Sprite Icon;

    public int Price { get; set; } = 100;

    public FullRank FullRank = new FullRank(ERank.Street_Rat, 0);

    public StorageEntity StorageEntity { get; set; } = new StorageEntity();
    public StorableItemDefinition ItemDefinition;
    public BackpackListing ShopListing;
    public ItemInstance ItemInstance;

    public Backpack() { }

    public Backpack(string name, string description, int rows, int columns, int price, FullRank fullRank, string iconPath)
    {
        Name = name;
        Description = description;
        Rows = rows;
        Columns = columns;
        Price = price;
        FullRank = fullRank;


        if (!ResourceUtils.TryLoadTexture("Backpack.Assets." + iconPath, out var backpackIcon))
        {
            Logger.Error("Failed to load backpack texture.");
            return;
        }
        var icon = Sprite.Create(backpackIcon, new Rect(0, 0, backpackIcon.width, backpackIcon.height), new Vector2(0.5f, 0.5f));
        icon.name = name + " (Icon)";
        if (icon == null)
        {
            Logger.Error($"Icon '{iconPath}' not found!");
            return;
        }
        Icon = icon;
    }
}
