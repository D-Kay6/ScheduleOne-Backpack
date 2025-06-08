using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Shop;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Shop;
#endif

namespace Backpack.Patches;

[HarmonyPatch(typeof(ShopInterface))]
public static class ShopInterfacePatch
{
    [HarmonyPatch("GetAvailableSlots")]
    [HarmonyPostfix]
#if IL2CPP
    public static void GetAvailableSlots(ShopInterface __instance, ref Il2CppSystem.Collections.Generic.List<ItemSlot> __result)
#elif MONO
    public static void GetAvailableSlots(ShopInterface __instance, ref List<ItemSlot> __result)
#endif
    {
        if (!PlayerBackpack.Instance.IsUnlocked)
            return;

        var loadingBayVehicle = __instance.GetLoadingBayVehicle();
        if (loadingBayVehicle != null && __instance.Cart.LoadVehicleToggle.isOn)
            return;

        var insertIndex = PlayerSingleton<PlayerInventory>.Instance.hotbarSlots.Count;
        var items = PlayerBackpack.Instance.ItemSlots;
        for (var i = 0; i < items.Count; i++)
        {
#if IL2CPP
            var itemSlot = items[new Index(i)].TryCast<ItemSlot>();
#elif MONO
            var itemSlot = items[i];
#endif
            if (itemSlot == null)
                continue;

            __result.Insert(i + insertIndex, itemSlot);
        }
    }
}