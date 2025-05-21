using HarmonyLib;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Shop;

namespace Backpack.Patches;

[HarmonyPatch(typeof(Cart))]
public static class CartPatch
{
    [HarmonyPatch("GetWarning")]
    [HarmonyPostfix]
    public static void GetWarning(Cart __instance, ref bool __result, ref string warning)
    {
        if (!PlayerBackpack.Instance.IsUnlocked)
            return;

        if (warning.StartsWith("Vehicle") || !__result)
            return;

        var backpack = Player.Local.GetBackpackStorage();
        if (!__instance.Shop.WillCartFit(backpack.ItemSlots))
            return;

        warning = "Inventory won't fit everything. Some items will be placed in your backpack.";
    }
}