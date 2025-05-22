using Il2CppScheduleOne.UI.Shop;

namespace Backpack;
public class ShopManager
{
    readonly ShopInterface _shop;

    public ShopManager()
    {
        var shopUIName = "HardwareStoreInterface (North Store)";
        var shopManager = UnityEngine.GameObject.Find(shopUIName);
        if (shopManager == null)
        {
            Logger.Error($"Shop manager '{shopUIName}' not found.");
        }
        _shop = shopManager.GetComponent<ShopInterface>();
        for (int i = 0; i < BackpackMod.Backpacks.Count; i++)
        {
            var backpack = BackpackMod.Backpacks[i];
            var listing = backpack.ShopListing;

            Logger.Info($"Backpack \"{backpack.Name}\" added to Dan's Hardware shop.");
            _shop.Listings.Add(listing);
        }
    }
}
