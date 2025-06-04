using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Shop;
#elif MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Shop;
#endif

namespace Backpack.Patches;

[HarmonyPatch(typeof(Cart))]
public static class CartPatch
{
    private static readonly List<ShopListing> _purchasedBackpacks = [];

    [HarmonyPatch("Buy")]
    [HarmonyPrefix]
    public static void BeforeCartBuy(Cart __instance)
    {
        _purchasedBackpacks.Clear();
        for (int i = 0; i < __instance.cartEntries.Count; i++)
        {
            var entry = __instance.cartEntries[i];
            if (BackpackMod.Backpacks.Any(x => x.ShopListing.name == entry.Listing.name))
            {
                _purchasedBackpacks.Add(entry.Listing);
            }
        }
    }

    [HarmonyPatch("Buy")]
    [HarmonyPostfix]
    public static void AfterCartBuy(Cart __instance)
    {
        for (int i = 0; i < _purchasedBackpacks.Count; i++)
        {
            var backpack = BackpackMod.Backpacks.FirstOrDefault(x => x.ShopListing.name == _purchasedBackpacks[i].name);
            PlayerBackpack.Instance.EquipBackpackByName(backpack.Name);
            PlayerInventory.Instance.RemoveAmountOfItem(backpack.ItemDefinition.ID);
        }
    }

    [HarmonyPatch("GetWarning")]
    [HarmonyPostfix]
    public static void GetWarning(Cart __instance, ref bool __result, ref string warning)
    {
        if (!PlayerBackpack.Instance.IsUnlocked)
            return;

        if (warning.StartsWith("Vehicle") || !__result)
            return;

        var items = PlayerBackpack.Instance.ItemSlots;
#if IL2CPP
        items.InsertRange(0, PlayerInventory.Instance.hotbarSlots.Cast<Il2CppSystem.Collections.Generic.IEnumerable<ItemSlot>>());
#elif MONO
        items.InsertRange(0, PlayerInventory.Instance.hotbarSlots);
#endif
        if (!__instance.Shop.WillCartFit(items))
            return;

        warning = "Inventory won't fit everything. Some items will be placed in your backpack.";
    }

    [HarmonyPatch("CanCheckout")]
    [HarmonyPostfix]
    public static void CanCheckout(Cart __instance, ref bool __result, ref string reason)
    {
        if (!PlayerBackpack.Instance.IsUnlocked || !__result)
            return;

        int totalBackpacks = 0;
        for (int i = 0; i < __instance.cartEntries.Count; i++)
        {
            var entry = __instance.cartEntries[i];
            var backpack = BackpackMod.Backpacks.Find(x => x.ShopListing.name == entry.Listing.name);
            if (backpack != null)
            {
                var currentBackpack = PlayerBackpack.Instance.GetCurrentBackpack();
                if (currentBackpack?.Slots > backpack.Slots)
                {
                    reason = "You cannot buy a backpack with less slots than your current one.";
                    __result = false;
                    break;
                }
                totalBackpacks++;
            }
            if (totalBackpacks > 1)
            {
                reason = "You can only buy one backpack at a time.";
                __result = false;
                break;
            }
        }
    }
}
