using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;

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
    public static void WriteData(Player __instance, string parentFolderPath)
    {
        var backpackStorage = __instance.GetBackpackStorage();
        var contents = new ItemSet(backpackStorage.ItemSlots).GetJSON();
        __instance.Cast<ISaveable>().WriteSubfile(parentFolderPath, "Backpack", contents);
    }

    [HarmonyPatch("Load", typeof(PlayerData), typeof(string))]
    [HarmonyPrefix]
    public static void Load(Player __instance, PlayerData data, string containerPath)
    {
        if (!__instance.Loader.TryLoadFile(containerPath, "Backpack", out var backpackData))
            return;

        Logger.Info("Loading local backpack data.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            var itemSet = ItemSet.Deserialize(backpackData);
            backpackStorage.LoadFromItemSet(itemSet);
        }
        catch (Exception e)
        {
            Logger.Error($"Error loading backpack data: {e.Message}");
        }
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
            var itemSet = ItemSet.Deserialize(backpackData);
            backpackStorage.LoadFromItemSet(itemSet);
        }
        catch (Exception e)
        {
            Logger.Error($"Error loading backpack data: {e.Message}");
        }
    }

    [HarmonyPatch("Activate")]
    [HarmonyPrefix]
    public static void Activate()
    {
        Logger.Info("Activating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("Deactivate")]
    [HarmonyPrefix]
    public static void Deactivate()
    {
        Logger.Info("Deactivating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    public static void ExitAll()
    {
        Logger.Info("Exiting all backpacks");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("PassOutRecovery")]
    [HarmonyPrefix]
    public static void PassOutRecovery()
    {
        PlayerBackpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("PassOut")]
    [HarmonyPrefix]
    public static void PassOut()
    {
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("OnRevived")]
    [HarmonyPrefix]
    public static void OnRevived()
    {
        PlayerBackpack.Instance.SetBackpackEnabled(true);
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