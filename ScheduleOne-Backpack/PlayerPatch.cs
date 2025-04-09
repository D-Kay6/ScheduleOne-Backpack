using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod;

[HarmonyPatch(typeof(Player))]
public static class PlayerPatch
{
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
    [HarmonyWrapSafe]
    public static void WriteData(Player __instance, string parentFolderPath)
    {
        var backpackStorage = PlayerBackpack.Instance.GetBackpackString();
        __instance.Cast<ISaveable>().WriteSubfile(parentFolderPath, "Backpack", backpackStorage);
    }

    [HarmonyPatch("Load", typeof(PlayerData), typeof(string))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Load(Player __instance, PlayerData data, string containerPath)
    {
        if (!__instance.Loader.TryLoadFile(containerPath, "Backpack", out var contentsString))
            return;

        try
        {
            PlayerBackpack.Instance.LoadBackpack(contentsString);
        }
        catch (Exception e)
        {
            Melon<BackpackMod>.Logger.Error($"Error loading backpack data: {e.Message}");
        }
    }

    [HarmonyPatch("Activate")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Activate()
    {
        Melon<BackpackMod>.Logger.Msg("Activating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("Deactivate")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Deactivate()
    {
        Melon<BackpackMod>.Logger.Msg("Deactivating backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void ExitAll()
    {
        Melon<BackpackMod>.Logger.Msg("Exiting all backpacks");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void OnDied(Player __instance)
    {
        if (!__instance.Owner.IsLocalClient)
            return;

        Melon<BackpackMod>.Logger.Msg("Player died, disabling backpack");
        PlayerBackpack.Instance.SetBackpackEnabled(false);
    }
}