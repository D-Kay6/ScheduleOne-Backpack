using HarmonyLib;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;

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

        BackpackMod.Log.LogInfo("Registering backpack file for player.");
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

        BackpackMod.Log.LogInfo("Loading local backpack data.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            var itemSet = ItemSet.Deserialize(backpackData);
            backpackStorage.LoadFromItemSet(itemSet);
        }
        catch (Exception e)
        {
            BackpackMod.Log.LogError($"Error loading backpack data: {e.Message}");
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
            BackpackMod.Log.LogInfo("Not the owner, skipping backpack data load.");
            return;
        }

        var backpackString = contentsString.Split(["|||"], StringSplitOptions.None);
        if (backpackString.Length < 2)
            return;

        contentsString = backpackString[0];
        var backpackData = backpackString[1];
        BackpackMod.Log.LogInfo("Loading backpack data from network.");
        try
        {
            var backpackStorage = __instance.GetBackpackStorage();
            var itemSet = ItemSet.Deserialize(backpackData);
            backpackStorage.LoadFromItemSet(itemSet);
        }
        catch (Exception e)
        {
            BackpackMod.Log.LogError($"Error loading backpack data: {e.Message}");
        }
    }

    [HarmonyPatch("Activate")]
    [HarmonyPrefix]
    public static void Activate()
    {
        BackpackMod.Log.LogInfo("Activating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("Deactivate")]
    [HarmonyPrefix]
    public static void Deactivate()
    {
        BackpackMod.Log.LogInfo("Deactivating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    public static void ExitAll()
    {
        BackpackMod.Log.LogInfo("Exiting all backpacks");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    public static void OnDied(Player __instance)
    {
        if (!__instance.Owner.IsLocalClient)
            return;

        BackpackMod.Log.LogInfo("Player died, disabling backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }
}