using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.PlayerScripts;
#elif MONO
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.PlayerScripts;
#endif

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

#if IL2CPP
        var dataPath = (Il2CppSystem.String) __instance.loadedPlayerDataPaths[new Index(__instance.loadedPlayerData.IndexOf(data))];
#elif MONO
        var dataPath = __instance.loadedPlayerDataPaths[__instance.loadedPlayerData.IndexOf(data)];
#endif
        var loader = new PlayerLoader();
        if (!loader.TryLoadFile(dataPath, "Backpack", out var backpackString))
        {
            Logger.Warning("Failed to load player backpack under " + dataPath);
            return;
        }

        inventoryString += "|||" + backpackString;
    }
}