using HarmonyLib;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(Player))]
public static class PlayerPatch
{
    [HarmonyPatch("NetworkInitializeIfDisabled")]
    [HarmonyPrefix]
    public static void NetworkInitializeIfDisabled(Player __instance)
    {
        var backpackStorage = __instance.GetBackpackStorage();
        if (backpackStorage == null)
            return;

        backpackStorage.NetworkInitializeIfDisabled_Internal();
    }

    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void Awake(Player __instance)
    {
        if (__instance.LocalExtraFiles.Contains("Backpack"))
        {
            Melon<BackpackMod>.Logger.Msg("Player already has backpack data.");
            return;
        }

        Melon<BackpackMod>.Logger.Msg("Adding backpack data to player.");
        __instance.LocalExtraFiles.Add("Backpack");
    }

    [HarmonyPatch("WriteData")]
    [HarmonyPostfix]
    public static void WriteData(Player __instance, string parentFolderPath)
    {
        var backpackStorage = __instance.GetBackpackStorage();
        __instance.Cast<ISaveable>().WriteSubfile(parentFolderPath, "Backpack", backpackStorage.GetContentString());
    }

    [HarmonyPatch("GetNetworth")]
    [HarmonyPostfix]
    public static void GetNetworth(Player __instance, MoneyManager.FloatContainer container)
    {
        var backpackStorage = __instance.GetBackpackStorage();
        if (backpackStorage == null)
            return;

        backpackStorage.GetNetworth(container);
    }

    [HarmonyPatch("Load", typeof(PlayerData), typeof(string))]
    [HarmonyPrefix]
    public static void Load(Player __instance, PlayerData data, string containerPath)
    {
        if (!__instance.Loader.TryLoadFile(containerPath, "Backpack", out var contentsString))
            return;

        Melon<BackpackMod>.Logger.Msg("Loading local backpack data.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            backpackStorage.LoadContents(contentsString);
        }
        catch (Exception e)
        {
            Melon<BackpackMod>.Logger.Error($"Error loading backpack data: {e.Message}");
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
            Melon<BackpackMod>.Logger.Msg("Not the owner, skipping backpack data load.");
            return;
        }

        var backpackString = contentsString.Split(["|||"], StringSplitOptions.None);
        if (backpackString.Length < 2)
            return;

        contentsString = backpackString[0];
        var backpackData = backpackString[1];
        Melon<BackpackMod>.Logger.Msg("Loading backpack data from network.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            backpackStorage.LoadContents(backpackData);
        }
        catch (Exception e)
        {
            Melon<BackpackMod>.Logger.Error($"Error loading backpack data: {e.Message}");
        }
    }

    [HarmonyPatch("Activate")]
    [HarmonyPrefix]
    public static void Activate()
    {
        Melon<BackpackMod>.Logger.Msg("Activating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("Deactivate")]
    [HarmonyPrefix]
    public static void Deactivate()
    {
        Melon<BackpackMod>.Logger.Msg("Deactivating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    public static void ExitAll()
    {
        Melon<BackpackMod>.Logger.Msg("Exiting all backpacks");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    public static void OnDied(Player __instance)
    {
        if (!__instance.Owner.IsLocalClient)
            return;

        Melon<BackpackMod>.Logger.Msg("Player died, disabling backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }
}