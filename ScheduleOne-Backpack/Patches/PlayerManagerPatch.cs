using HarmonyLib;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.PlayerScripts;

namespace Backpack.Patches;

[HarmonyPatch(typeof(PlayerManager))]
public static class PlayerManagerPatch
{
    [HarmonyPatch("TryGetPlayerData")]
    [HarmonyPostfix]
    public static void TryGetPlayerData(PlayerManager __instance, PlayerData data, ref string inventoryString)
    {
        if (data == null)
            return;

        var dataPath = (Il2CppSystem.String) __instance.loadedPlayerDataPaths[new Index(__instance.loadedPlayerData.IndexOf(data))];
        var loader = new PlayerLoader();
        if (!loader.TryLoadFile(dataPath, "Backpack", out var backpackString))
        {
            Logger.Warning("Failed to load player backpack under " + dataPath);
            return;
        }

        inventoryString += "|||" + backpackString;
    }
}