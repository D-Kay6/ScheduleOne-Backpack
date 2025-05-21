using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI;
using UnityEngine;

namespace Backpack.Patches;

[HarmonyPatch(typeof(StorageMenu))]
public static class StorageMenuPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void Awake(StorageMenu __instance)
    {
        if (__instance.SlotsUIs.Length >= Configuration.Instance.StorageSlots)
            return;

        var container = __instance.SlotContainer;
        var prefab = __instance.SlotsUIs[0].gameObject;

        var slots = new ItemSlotUI[Configuration.Instance.StorageSlots];
        for (var i = 0; i < Configuration.Instance.StorageSlots; i++)
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

    [HarmonyPatch("Open", [typeof(string), typeof(string), typeof(IItemSlotOwner)])]
    [HarmonyPostfix]
    public static void Open(StorageMenu __instance, string title, string subtitle, IItemSlotOwner owner)
    {
        __instance.CloseButton.anchoredPosition = new Vector2(0f, __instance.SlotGridLayout.constraintCount * -(__instance.SlotGridLayout.cellSize.y + __instance.SlotGridLayout.spacing.y) - __instance.CloseButton.sizeDelta.y);
        if (__instance.SlotGridLayout.constraintCount <= 4)
            return;

        var pos = __instance.transform.position;
        __instance.Container.position = new Vector3(pos.x, pos.y - __instance.SlotGridLayout.constraintCount * -(__instance.SlotGridLayout.cellSize.y - __instance.SlotGridLayout.spacing.y) - __instance.CloseButton.sizeDelta.y, pos.z);
    }

    [HarmonyPatch("CloseMenu")]
    [HarmonyPrefix]
    public static void CloseMenu(StorageMenu __instance)
    {
        __instance.Container.position = __instance.transform.position;
    }
}