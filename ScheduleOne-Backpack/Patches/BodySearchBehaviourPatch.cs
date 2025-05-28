using System.Collections;
using Backpack.Config;
using HarmonyLib;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;

namespace Backpack.Patches;

[HarmonyPatch(typeof(BodySearchBehaviour))]
public static class BodySearchBehaviourPatch
{
    [HarmonyPatch("SearchClean")]
    [HarmonyPrefix]
    public static bool SearchClean(BodySearchBehaviour __instance)
    {
        if (!PlayerBackpack.Instance.IsUnlocked || !Configuration.Instance.EnableSearch)
            return true;

        BodySearchScreen.Instance.onSearchClear.RemoveListener(new Action(__instance.SearchClean));
        BodySearchScreen.Instance.onSearchFail.RemoveListener(new Action(__instance.SearchFail));
        BodySearchScreen.Instance.IsOpen = true; // This prevents the inventory search screen from opening again.
        MelonCoroutines.Start(CheckForItems(__instance));
        return false;
    }

    private static IEnumerator CheckForItems(BodySearchBehaviour behaviour)
    {
        behaviour.officer.dialogueHandler.ShowWorldspaceDialogue("Hold on, let me see your backpack as well.", 5f);
        yield return new WaitForSeconds(3f);
        BodySearchScreen.Instance.IsOpen = false;
        behaviour.ConcludeSearch(!PlayerBackpack.Instance.ContainsItemsOfInterest(behaviour.MaxStealthLevel));
    }
}