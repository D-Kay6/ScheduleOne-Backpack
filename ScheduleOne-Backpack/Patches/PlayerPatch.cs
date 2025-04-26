using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(Player))]
public static class PlayerPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void Awake(Player __instance)
    {
        var backpackStorage = __instance.GetBackpackStorage();
        if (backpackStorage)
        {
            // Failsafe
            backpackStorage.SlotCount = 12;
            backpackStorage.DisplayRowCount = 3;
            backpackStorage.StorageEntityName = PlayerBackpack.StorageName;
            backpackStorage.MaxAccessDistance = float.PositiveInfinity;
        }

        if (__instance.LocalExtraFiles.Contains("Backpack"))
            return;

        Melon<BackpackMod>.Logger.Msg("Registering backpack file for player.");
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

        Melon<BackpackMod>.Logger.Msg("Loading local backpack data.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            var itemSet = ItemSet.Deserialize(backpackData);
            backpackStorage.LoadFromItemSet(itemSet);
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
            var itemSet = ItemSet.Deserialize(backpackData);
            backpackStorage.LoadFromItemSet(itemSet);
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