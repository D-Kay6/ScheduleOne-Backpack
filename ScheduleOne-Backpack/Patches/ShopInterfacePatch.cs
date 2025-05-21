using HarmonyLib;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Shop;

namespace Backpack.Patches;

[HarmonyPatch(typeof(ShopInterface))]
public static class ShopInterfacePatch
{
    [HarmonyPatch("GetAvailableSlots")]
    [HarmonyPostfix]
    public static void GetAvailableSlots(ShopInterface __instance, ref Il2CppSystem.Collections.Generic.List<ItemSlot> __result)
    {
        if (!PlayerBackpack.Instance.IsUnlocked)
            return;

        var loadingBayVehicle = __instance.GetLoadingBayVehicle();
        if (loadingBayVehicle != null && __instance.Cart.LoadVehicleToggle.isOn)
            return;

        var insertIndex = PlayerSingleton<PlayerInventory>.instance.hotbarSlots.Count;
        var backpack = Player.Local.GetBackpackStorage();
        for (var i = 0; i < backpack.ItemSlots.Count; i++)
        {
            var itemSlot = backpack.ItemSlots[new Index(i)].Cast<ItemSlot>();
            if (itemSlot == null)
                continue;

            __result.Insert(i + insertIndex, itemSlot);
        }
    }
}