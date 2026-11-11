using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using SaveFileList = Il2CppSystem.Collections.Generic.List<string>;
#elif MONO
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using SaveFileList = System.Collections.Generic.List<string>;
#endif

namespace Backpack.Patches;

[HarmonyPatch(typeof(Player))]
public static class PlayerPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void Awake(Player __instance)
    {
        if (__instance.LocalExtraFiles.Contains("Backpack"))
            return;

        Logger.Info("Registering backpack file for player.");
        __instance.LocalExtraFiles.Add("Backpack");
    }

    [HarmonyPatch("WriteData")]
    [HarmonyPostfix]
    public static void WriteData(Player __instance, string parentFolderPath, SaveFileList __result)
    {
        var backpackStorage = __instance.GetBackpackStorage();
        if (backpackStorage == null)
            return;

        var contents = new ItemSet(backpackStorage.ItemSlots).GetJSON();

#if IL2CPP
        var written = __instance.Cast<ISaveable>().WriteSubfile(parentFolderPath, "Backpack", contents);
#elif MONO
        ISaveable instance = __instance;
        var written = instance.WriteSubfile(parentFolderPath, "Backpack", contents);
#endif
        // Register the file as approved so the save system does not delete it as stale.
        __result?.Add(string.IsNullOrEmpty(written) ? "Backpack.json" : Path.GetFileName(written));
    }

    [HarmonyPatch("LoadInventory")]
    [HarmonyPrefix]
    public static void LoadInventory(Player __instance, ref string contentsString)
    {
        if (string.IsNullOrEmpty(contentsString))
            return;

        if (!__instance.IsOwner)
        {
            Logger.Info("Not the owner, skipping backpack data load.");
            return;
        }

        var backpackString = contentsString.Split(["|||"], StringSplitOptions.None);
        if (backpackString.Length < 2)
            return;

        contentsString = backpackString[0];
        var backpackData = backpackString[1];
        Logger.Info("Loading backpack data from network.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            if (!ItemSet.TryDeserialize(backpackData, out var itemSet))
            {
                Logger.Error("Failed to deserialize backpack data.");
                return;
            }

            itemSet.LoadTo(backpackStorage.ItemSlots);
        }
        catch (Exception e)
        {
            Logger.Error($"Error loading backpack data: {e.Message}");
        }
    }

    // Player.Activate/Deactivate were removed in game 0.4.x. The backpack is enabled by default and
    // only disabled on death/exit, so re-enable it when the local player is revived.
    [HarmonyPatch("OnRevived")]
    [HarmonyPostfix]
    public static void OnRevived(Player __instance)
    {
        if (__instance.Owner == null || !__instance.Owner.IsLocalClient)
            return;

        Logger.Info("Player revived, enabling backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    public static void ExitAll()
    {
        // Close only; with Activate gone there is no hook that would re-enable a disabled backpack.
        PlayerBackpack.Instance?.Close();
    }

    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    public static void OnDied(Player __instance)
    {
        if (!__instance.Owner.IsLocalClient)
            return;

        Logger.Info("Player died, disabling backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }
}