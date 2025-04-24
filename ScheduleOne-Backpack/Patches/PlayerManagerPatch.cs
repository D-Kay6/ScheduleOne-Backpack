using HarmonyLib;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.PlayerScripts;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(PlayerManager))]
public static class PlayerManagerPatch
{
    [HarmonyPatch("TryGetPlayerData")]
    [HarmonyPostfix]
    public static void TryGetPlayerData(ref PlayerManager __instance, ref bool __result, string playerCode, PlayerData data, ref string inventoryString)
    {
        if (!__result)
            return;

        var dataPath = (Il2CppSystem.String) __instance.loadedPlayerDataPaths[new Index(__instance.loadedPlayerData.IndexOf(data))];
        var loader = new PlayerLoader();
        if (!loader.TryLoadFile(dataPath, "Backpack", out var backpackString))
            return;

        inventoryString += "|||" + backpackString;
    }
}