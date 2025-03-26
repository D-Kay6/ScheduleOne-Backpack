using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using System.Collections;
using System;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMain), "Backpack Mod", "1.0.0", "Tugakit")]
[assembly: MelonGame("DrugLords", "Schedule One")]

namespace BackpackMod
    {
    public class BackpackMain : MelonMod
        {
        // Configuration
        private const int ROWS = 4;
        private const int COLUMNS = 3;
        private const KeyCode TOGGLE_KEY = KeyCode.B;

        // Storage references
        private StorageEntity backpackStorage;
        private StorageMenu storageMenu;
        private bool isInitialized = false;

        public override void OnInitializeMelon()
            {
            LoggerInstance.Msg("Backpack Mod initialized");
            }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
            {
            // Only initialize in game scenes
            if (!isInitialized)
                {
                MelonCoroutines.Start(DelayedInit());
                }
            }

        public override void OnUpdate()
            {
            // Toggle backpack with B key
            if (Input.GetKeyDown(TOGGLE_KEY) && backpackStorage != null)
                {
                try
                    {
                    if (backpackStorage.IsOpened)
                        {
                        backpackStorage.Close();
                        LoggerInstance.Msg("Backpack closed");
                        }
                    else
                        {
                        backpackStorage.Open();
                        LoggerInstance.Msg("Backpack opened");
                        }
                    }
                catch (Exception ex)
                    {
                    LoggerInstance.Error($"Error toggling backpack: {ex.Message}");
                    }
                }
            }

        private IEnumerator DelayedInit()
            {
            // Wait for other systems to initialize
            for (int i = 0; i < 20; i++) yield return null;

            try
                {
                LoggerInstance.Msg("Setting up backpack...");

                // Find the storage menu
                storageMenu = UnityEngine.Object.FindObjectOfType<StorageMenu>();
                if (storageMenu == null)
                    {
                    LoggerInstance.Error("StorageMenu not found");
                    yield break;
                    }

                LoggerInstance.Msg("Found StorageMenu");

                // Find a StorageEntity to use as a template
                var storageEntities = UnityEngine.Object.FindObjectsOfType<StorageEntity>();
                if (storageEntities == null || storageEntities.Length == 0)
                    {
                    LoggerInstance.Error("No storage entities found");
                    yield break;
                    }

                // Choose a template
                StorageEntity template = null;
                foreach (var entity in storageEntities)
                    {
                    if (entity.SlotCount > 0)
                        {
                        template = entity;
                        LoggerInstance.Msg($"Found template: {template.name} with {template.SlotCount} slots");
                        break;
                        }
                    }

                if (template == null)
                    {
                    template = storageEntities[0];
                    LoggerInstance.Msg($"Using fallback template: {template.name}");
                    }

                // Create backpack
                backpackStorage = UnityEngine.Object.Instantiate(template);
                backpackStorage.name = "BackpackStorage";
                UnityEngine.Object.DontDestroyOnLoad(backpackStorage.gameObject);

                // Configure backpack
                backpackStorage.StorageEntityName = "Backpack";
                backpackStorage.StorageEntitySubtitle = "Personal Storage";
                backpackStorage.SlotCount = ROWS * COLUMNS;
                backpackStorage.DisplayRowCount = COLUMNS;
                backpackStorage.MaxAccessDistance = 999f; // Always accessible
                backpackStorage.AccessSettings = StorageEntity.EAccessSettings.Full;
                backpackStorage.onOpened.AddListener(new Action(() => {
                    // Find the storage menu when opened
                    var currentMenu = UnityEngine.Object.FindObjectOfType<StorageMenu>();
                    if (currentMenu != null)
                        {
                        // Basic grid layout fix as before
                        if (currentMenu.SlotGridLayout != null)
                            {
                            currentMenu.SlotGridLayout.constraintCount = COLUMNS;
                            currentMenu.SlotGridLayout.childAlignment = TextAnchor.UpperCenter;
                            }

                        // Make all slots visible
                        if (currentMenu.SlotsUIs != null)
                            {
                            for (int i = 0; i < currentMenu.SlotsUIs.Length; i++)
                                {
                                if (currentMenu.SlotsUIs[i] != null)
                                    {
                                    currentMenu.SlotsUIs[i].SetVisible(true);
                                    }
                                }
                            }
                        }
                }));
                // Fix for IL2CPP interface conversion issue - we'll use the existing Open method
                // instead of manually calling properties that cause type conversion errors

                LoggerInstance.Msg("Backpack setup complete!");
                var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
                for (int i = 0; i < ROWS * COLUMNS; i++)
                    {
                    var slot = new ItemSlot();
                    slots.Add(slot);
                    }
                backpackStorage.ItemSlots = slots;
                LoggerInstance.Msg($"Created {slots.Count} slots for backpack");
                isInitialized = true;
                }
            catch (Exception ex)
                {
                LoggerInstance.Error($"Error setting up backpack: {ex.Message}");
                LoggerInstance.Error(ex.StackTrace);
                }
            }
        }
    }
