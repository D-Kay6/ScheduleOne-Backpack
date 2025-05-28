using Backpack.Config;
using HarmonyLib;
using Il2CppScheduleOne.Levelling;
using UnityEngine;

namespace Backpack.Patches;

[HarmonyPatch(typeof(LevelManager))]
public static class LevelManagerPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void Awake(LevelManager __instance)
    {
        if (__instance == null)
        {
            Logger.Error("LevelManager instance is null!");
            return;
        }

        if (!ResourceUtils.TryLoadTexture("Backpack.Assets.Icon.png", out var backpackIcon))
        {
            Logger.Error("Failed to load backpack icon texture.");
            return;
        }

        var backpackSprite = Sprite.Create(backpackIcon, new Rect(0, 0, backpackIcon.width, backpackIcon.height), new Vector2(0.5f, 0.5f));
        var unlockable = new Unlockable(Configuration.Instance.UnlockLevel, "Backpack", backpackSprite);
        __instance.AddUnlockable(unlockable);
    }
}