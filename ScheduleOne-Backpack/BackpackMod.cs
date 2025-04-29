using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace BackpackMod;

[BepInPlugin("D-Kay-PlayerBackpack", "BackpackMod", "1.5.1")]
public class BackpackMod : BasePlugin
{
    internal static new ManualLogSource Log;
    private static Harmony harmony;
    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("BackpackMod initialized.");

        ClassInjector.RegisterTypeInIl2Cpp<PlayerBackpack>();

        harmony = new Harmony($"D-Kay-PlayerBackpack");
        harmony.PatchAll();
    }
}