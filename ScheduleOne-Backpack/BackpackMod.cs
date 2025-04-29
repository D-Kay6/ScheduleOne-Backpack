using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace BackpackMod;

[BepInPlugin("dkay.schedule1.backpack", "BackpackMod", "1.5.1")]
public class BackpackMod : BasePlugin
{
    internal static new ManualLogSource Log;
    private static Harmony harmony;
    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("BackpackMod initialized.");

        ClassInjector.RegisterTypeInIl2Cpp<PlayerBackpack>();

        harmony = new Harmony("dkay.schedule1.backpack");
        harmony.PatchAll();
    }
}