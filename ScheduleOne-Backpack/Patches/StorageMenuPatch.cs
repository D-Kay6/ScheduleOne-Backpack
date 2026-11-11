using HarmonyLib;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI;
#elif MONO
using ScheduleOne.ItemFramework;
using ScheduleOne.UI;
#endif

namespace Backpack.Patches;

[HarmonyPatch(typeof(StorageMenu))]
public static class StorageMenuPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void Awake(StorageMenu __instance)
    {
        if (__instance.SlotsUIs.Length >= PlayerBackpack.MaxStorageSlots)
            return;

        var container = __instance.SlotContainer;
        var prefab = __instance.SlotsUIs[0]?.gameObject;
        if (prefab == null)
        {
            MelonLoader.MelonLogger.Error("StorageMenu prefab is null. Cannot create additional slots.");
            return;
        }

        var slots = new ItemSlotUI[PlayerBackpack.MaxStorageSlots];
        for (var i = 0; i < PlayerBackpack.MaxStorageSlots; i++)
        {
            if (i < __instance.SlotsUIs.Length)
            {
                slots[i] = __instance.SlotsUIs[i];
                continue;
            }

            var slot = UnityEngine.Object.Instantiate(prefab, container);
            slot.name = $"{prefab.name} ({i})";
            slot.gameObject.SetActive(true);
            slots[i] = slot.GetComponent<ItemSlotUI>();
        }

        __instance.SlotsUIs = slots;
    }

#if IL2CPP
    [HarmonyPatch("Open", [typeof(string), typeof(string), typeof(IItemSlotOwner), typeof(Il2CppSystem.Action)])]
#elif MONO
    [HarmonyPatch("Open", [typeof(string), typeof(string), typeof(IItemSlotOwner), typeof(Action)])]
#endif
    [HarmonyPostfix]
    public static void Open(StorageMenu __instance)
    {
        var spacing = __instance.SlotGridLayout.cellSize.y + __instance.SlotGridLayout.spacing.y;
        __instance.CloseButtonContainer.anchoredPosition = new Vector2(0f, __instance.SlotGridLayout.constraintCount * -spacing - __instance.CloseButtonContainer.sizeDelta.y);
        if (__instance.SlotGridLayout.constraintCount <= 4)
            return;

        __instance.Container.localPosition = new Vector3(0f, (__instance.SlotGridLayout.constraintCount - 4) * spacing, 0f);
    }

    [HarmonyPatch("CloseMenu")]
    [HarmonyPrefix]
    public static void CloseMenu(StorageMenu __instance)
    {
        __instance.Container.localPosition = Vector3.zero;
    }
}