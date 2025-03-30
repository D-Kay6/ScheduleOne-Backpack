using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppScheduleOne.Storage;
using System.Collections.Generic;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.ItemFramework;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMain), "BackpackMod", "1.2.0", "Tugakit")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod
    {
    public class BackpackMain : MelonMod
        {
        private const int ROWS = 4;
        private const int COLUMNS = 3;
        private const KeyCode TOGGLE_KEY = KeyCode.B;

        private StorageEntity backpackEntity;
        private bool isInitialized = false;

        public override void OnInitializeMelon()
            {
            MelonLogger.Msg("BackpackMod initialized.");
            MelonLogger.Error("--WARNING--", "Items in this backpack will be LOST when you exit the game or the save!", 5f);
            }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
            {
            // Only set up the backpack in the "Main" scene.
            if (sceneName != "Main")
                {
                // If we already had a backpack, disable it so it won't break in the menu.
                if (backpackEntity != null)
                    backpackEntity.gameObject.SetActive(false);

                MelonLogger.Msg($"Scene '{sceneName}' detected. Backpack is disabled in non-gameplay scenes.");
                return;
                }
            MelonCoroutines.Start(SetupBackpack());
            }

        private System.Collections.IEnumerator SetupBackpack()
            {
            yield return new WaitForSeconds(1f);

            try
                {
                var templates = UnityEngine.Object.FindObjectsOfType<StorageEntity>();
                if (templates == null || templates.Length == 0)
                    {
                    MelonLogger.Error("No StorageEntity template found in scene!");
                    yield break;
                    }

                backpackEntity = UnityEngine.Object.Instantiate(templates[0]);
                backpackEntity.name = "BackpackStorage";
                backpackEntity.StorageEntityName = "Backpack";
                backpackEntity.SlotCount = ROWS * COLUMNS;
                backpackEntity.DisplayRowCount = COLUMNS;
                backpackEntity.MaxAccessDistance = 999f;
                backpackEntity.AccessSettings = StorageEntity.EAccessSettings.Full;

                var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
                for (int i = 0; i < ROWS * COLUMNS; i++)
                    slots.Add(new ItemSlot());
                backpackEntity.ItemSlots = slots;


                isInitialized = true;
                MelonLogger.Msg($"Backpack setup complete with {ROWS * COLUMNS} slots.");
                }
            catch (Exception ex)
                {
                MelonLogger.Error("Error setting up backpack: " + ex.Message);
                }
            }

        public override void OnUpdate()
            {
            // Only allow toggling in the "Main" scene.
            if (SceneManager.GetActiveScene().name != "Main")
                return;
            if (!isInitialized)
                return;

            if (Input.GetKeyDown(TOGGLE_KEY))
                {
                try
                    {
                    if (backpackEntity.IsOpened)
                        backpackEntity.Close();
                    else
                        backpackEntity.Open();
                    }
                catch (Exception ex)
                    {
                    MelonLogger.Error("Error toggling backpack: " + ex.Message);
                    }
                }
            }
        }
    }
