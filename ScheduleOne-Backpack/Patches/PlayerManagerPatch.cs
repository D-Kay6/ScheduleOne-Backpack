using HarmonyLib;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.PlayerScripts;

namespace BackpackMod.Patches;

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
            BackpackMod.Log.LogWarning("Failed to load player backpack under " + dataPath);
            return;
        }

        inventoryString += "|||" + backpackString;
    }
}