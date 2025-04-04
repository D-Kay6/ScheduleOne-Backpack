using System.Collections;
using BackpackMod;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Storage;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[assembly:
    MelonInfo(typeof(BackpackMain), "BackpackMod", "1.2.1", "Tugakit", "https://www.nexusmods.com/schedule1/mods/107")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod;

public static class Backpack
{
    private const int Rows = 4;
    private const int Columns = 3;

    public static StorageEntity? Entity { get; private set; }

    public static IEnumerator Setup()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            var templates = Object.FindObjectsOfType<StorageEntity>();
            if (templates == null || templates.Length == 0)
            {
                MelonLogger.Error("No StorageEntity template found in scene!");
                yield break;
            }

            Entity = templates[0];
            Entity.name = "BackpackStorage";
            Entity.StorageEntityName = "Backpack";
            Entity.MaxAccessDistance = float.PositiveInfinity;
            Entity.AccessSettings = StorageEntity.EAccessSettings.Full;
            MelonLogger.Msg($"[{Entity.StorageEntityName}] - {Entity.name}");
            if (Entity.ItemSlots == null || Entity.ItemSlots.Count == 0) CreateSlots();
        }
        catch (Exception ex)
        {
            MelonLogger.Error("Error setting up backpack: " + ex.Message);
        }
    }

    private static void CreateSlots()
    {
        if (Entity == null) return;
        Entity.SlotCount = Rows * Columns;
        Entity.DisplayRowCount = Columns;

        var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
        var owner = Entity.TryCast<IItemSlotOwner>();
        if (owner == null)
        {
            MelonLogger.Error("Backpack entity is not a valid IItemSlotOwner.");
            return;
        }

        for (var i = 0; i < Rows * Columns; i++)
        {
            var slot = new ItemSlot { SlotOwner = owner };
            slots.Add(slot);
        }

        Entity.ItemSlots = slots;
    }

    public static void Disable()
    {
        if (Entity != null)
            Entity.gameObject.SetActive(false);
    }
}

public class BackpackMain : MelonMod
{
    private const KeyCode ToggleKey = KeyCode.B;
    private bool _isInitialized;

    public override void OnInitializeMelon() => MelonLogger.Msg("BackpackMod initialized.");


    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
        {
            Backpack.Disable();
            MelonLogger.Msg($"Scene '{sceneName}' detected. Backpack is disabled in non-gameplay scenes.");
            return;
        }

        MelonCoroutines.Start(InitializeBackpack());
    }

    private IEnumerator InitializeBackpack()
    {
        yield return Backpack.Setup();
        _isInitialized = true;
    }

    public override void OnUpdate()
    {
        if (SceneManager.GetActiveScene().name != "Main" || !_isInitialized || Backpack.Entity == null)
            return;

        if (!Input.GetKeyDown(ToggleKey)) return;
        try
        {
            if (Backpack.Entity.IsOpened)
                Backpack.Entity.Close();
            else
                Backpack.Entity.Open();
        }
        catch (Exception ex)
        {
            MelonLogger.Error("Error toggling backpack: " + ex.Message);
        }
    }
}