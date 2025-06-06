using System.Collections;
using Backpack.Config;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.UI;
#elif MONO
using System.Reflection.Emit;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.Police;
using ScheduleOne.UI;
#endif

namespace Backpack.Patches;

[HarmonyPatch(typeof(BodySearchBehaviour))]
public static class BodySearchBehaviourPatch
{
#if IL2CPP
    [HarmonyPatch("SearchClean")]
    [HarmonyPrefix]
    public static bool SearchClean(BodySearchBehaviour __instance)
    {
        if (!PlayerBackpack.Instance.IsBackpackEquipped || !Configuration.Instance.EnableSearch)
            return true;

        BodySearchScreen.Instance.onSearchClear.RemoveListener(new Action(__instance.SearchClean));
        BodySearchScreen.Instance.onSearchFail.RemoveListener(new Action(__instance.SearchFail));
        BodySearchScreen.Instance.IsOpen = true; // This prevents the inventory search screen from opening again.
        MelonCoroutines.Start(CheckForItems(__instance));
        return false;
    }
#elif MONO
    [HarmonyPatch("SearchClean")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SearchClean(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            if (code.Calls(AccessTools.Method(typeof(BodySearchBehaviour), nameof(BodySearchBehaviour.ConcludeSearch))))
            {
                yield return new CodeInstruction(OpCodes.Pop);
                yield return CodeInstruction.Call(typeof(BodySearchBehaviourPatch), nameof(SearchClean), [typeof(BodySearchBehaviour)]);
                continue;
            }

            yield return code;
        }
    }

    private static void SearchClean(BodySearchBehaviour behaviour)
    {
        if (!PlayerBackpack.Instance.IsUnlocked || !Configuration.Instance.EnableSearch)
        {
            behaviour.ConcludeSearch(true);
            return;
        }

        new Traverse(BodySearchScreen.Instance).Property<bool>("IsOpen").Value = true; // This prevents the inventory search screen from opening again.
        MelonCoroutines.Start(CheckForItems(behaviour));
    }
#endif

    private static IEnumerator CheckForItems(BodySearchBehaviour behaviour)
    {
#if IL2CPP
        behaviour.officer.dialogueHandler.ShowWorldspaceDialogue("Hold on, let me see your backpack as well.", 5f);
        yield return new WaitForSeconds(3f);
        BodySearchScreen.Instance.IsOpen = false;
#elif MONO
        new Traverse(behaviour).Field<PoliceOfficer>("officer").Value.dialogueHandler.ShowWorldspaceDialogue("Hold on, let me see your backpack as well.", 5f);
        yield return new WaitForSeconds(3f);
        new Traverse(BodySearchScreen.Instance).Property<bool>("IsOpen").Value = false;
#endif
        behaviour.ConcludeSearch(!PlayerBackpack.Instance.ContainsItemsOfInterest(behaviour.MaxStealthLevel));
    }
}