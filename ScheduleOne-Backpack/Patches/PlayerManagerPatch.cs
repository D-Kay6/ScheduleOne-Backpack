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
    private const string Separator = "|||";

    // Since game 0.4.x, TryGetPlayerData returns a FullPlayerData bundle (basic data + inventory string + ...)
    // that the host sends to the client. Append the backpack contents to its inventory string so the
    // Player.LoadInventory patch can split them back out on the owning client.
    [HarmonyPatch("TryGetPlayerData", [typeof(string), typeof(bool), typeof(FullPlayerData)], [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out])]
    [HarmonyPostfix]
    public static void TryGetPlayerData(PlayerManager __instance, bool __result, ref FullPlayerData data)
    {
        if (__result)
            AppendBackpack(__instance, data);
    }

#if IL2CPP
    // The async overload may resolve data without going through the synchronous one, so wrap its callback too.
    // AppendBackpack is idempotent, so double application is harmless.
    [HarmonyPatch("TryGetPlayerData", [typeof(string), typeof(bool), typeof(Il2CppSystem.Action<FullPlayerData>), typeof(Il2CppSystem.Action)])]
    [HarmonyPrefix]
    public static void TryGetPlayerDataAsync(PlayerManager __instance, ref Il2CppSystem.Action<FullPlayerData> onSuccess)
    {
        var original = onSuccess;
        var manager = __instance;
        onSuccess = (Il2CppSystem.Action<FullPlayerData>) new Action<FullPlayerData>(data =>
        {
            try
            {
                AppendBackpack(manager, data);
            }
            catch (Exception e)
            {
                Logger.Error($"Error appending backpack data: {e}");
            }

            original?.Invoke(data);
        });
    }
#endif

    internal static void AppendBackpack(PlayerManager manager, FullPlayerData data)
    {
        if (data == null || data.BasicData == null)
            return;

        var inventoryString = data.InventoryString ?? string.Empty;
        if (inventoryString.Contains(Separator))
            return; // already appended

        var index = manager.loadedPlayerData.IndexOf(data.BasicData);
        if (index < 0 || index >= manager.loadedPlayerDataPaths.Count)
        {
            Logger.Warning("Could not resolve save path for player data; backpack contents not loaded.");
            return;
        }

#if IL2CPP
        var dataPath = (string) manager.loadedPlayerDataPaths[index];
#elif MONO
        var dataPath = manager.loadedPlayerDataPaths[index];
#endif
        var loader = new PlayerLoader();
        if (!loader.TryLoadFile(dataPath, "Backpack", out var backpackString))
        {
            Logger.Warning("No player backpack found under " + dataPath);
            return;
        }

        data.InventoryString = inventoryString + Separator + backpackString;
    }
}
