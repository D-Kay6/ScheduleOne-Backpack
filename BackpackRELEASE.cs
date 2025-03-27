using MelonLoader;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.DevUtilities;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMain), "Advanced Backpack", "1.0.0", "Author")]
[assembly: MelonGame("DrugLords", "Schedule One")]

namespace BackpackMod
    {
    [Serializable]
    public class BackpackSaveData
        {
        public List<BackpackItemData> Items { get; set; } = new List<BackpackItemData>();
        }

    [Serializable]
    public class BackpackItemData
        {
        public string ID { get; set; }
        public int Quantity { get; set; }
        public int SlotIndex { get; set; }
        }

    public class BackpackMain : MelonMod
        {
        private const int BACKPACK_ROWS = 4;
        private const int BACKPACK_COLUMNS = 3;
        private const string BACKPACK_SAVE_FILE = "backpack_save.json";
        private const KeyCode TOGGLE_BACKPACK_KEY = KeyCode.B;

        private StorageEntity backpackStorage;
        private bool isInitialized = false;

        public override void OnInitializeMelon()
            {
            MelonLogger.Msg("Advanced Backpack Mod Initialized");
            }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
            {
            if (sceneName == "Main")
                {
                MelonCoroutines.Start(InitializeBackpack());
                }
            }

        private IEnumerator InitializeBackpack()
            {
            // Wait for game systems to initialize
            for (int i = 0; i < 10; i++) yield return null;

            try
                {
                // Find storage template
                var storageEntities = UnityEngine.Object.FindObjectsOfType<StorageEntity>();
                if (storageEntities == null || storageEntities.Length == 0)
                    {
                    MelonLogger.Error("No storage entities found");
                    yield break;
                    }

                // Use first available storage entity as template
                StorageEntity template = storageEntities[0];

                // Create backpack storage
                backpackStorage = UnityEngine.Object.Instantiate(template);
                backpackStorage.name = "BackpackStorage";
                backpackStorage.StorageEntityName = "Backpack";
                backpackStorage.StorageEntitySubtitle = "Personal Storage";
                backpackStorage.SlotCount = BACKPACK_ROWS * BACKPACK_COLUMNS;
                backpackStorage.DisplayRowCount = BACKPACK_COLUMNS;
                backpackStorage.MaxAccessDistance = 999f;
                backpackStorage.AccessSettings = StorageEntity.EAccessSettings.Full;

                // Initialize slots
                var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
                for (int i = 0; i < BACKPACK_ROWS * BACKPACK_COLUMNS; i++)
                    {
                    slots.Add(new ItemSlot());
                    }
                backpackStorage.ItemSlots = slots;

                // Add toggle key listener
                backpackStorage.onOpened.AddListener(new Action(() => {
                    MelonLogger.Msg("Backpack opened");
                }));

                backpackStorage.onClosed.AddListener(new Action(() => {
                    MelonLogger.Msg("Backpack closed, saving...");
                    SaveBackpackData();
                }));

                // Start delayed loading process
                MelonCoroutines.Start(DelayedLoadProcess());

                isInitialized = true;
                MelonLogger.Msg("Backpack initialized successfully");
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Backpack initialization failed: {ex.Message}");
                MelonLogger.Error(ex.StackTrace);
                }
            }

        private IEnumerator DelayedLoadProcess()
            {
            // Wait for LoadManager and SaveManager
            for (int i = 0; i < 50; i++)
                {
                if (Singleton<LoadManager>.Instance != null &&
                    Singleton<SaveManager>.Instance != null)
                    break;

                yield return new WaitForSeconds(0.1f);
                }

            // Wait for paths to be available
            for (int i = 0; i < 50; i++)
                {
                if (Singleton<LoadManager>.Instance != null &&
                   !string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                    break;

                yield return new WaitForSeconds(0.1f);
                }

            // Extra wait to ensure everything is ready
            yield return new WaitForSeconds(1.0f);

            // Load the backpack data
            LoadBackpackData();
            }

        public override void OnUpdate()
            {
            if (!isInitialized) return;

            // Toggle backpack
            if (Input.GetKeyDown(TOGGLE_BACKPACK_KEY))
                {
                ToggleBackpack();
                }
            }

        private void ToggleBackpack()
            {
            try
                {
                if (backpackStorage.IsOpened)
                    {
                    backpackStorage.Close();
                    }
                else
                    {
                    backpackStorage.Open();
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Backpack toggle error: {ex.Message}");
                }
            }

        private string GetPlayerFolder()
            {
            try
                {
                // Try LoadManager first
                if (Singleton<LoadManager>.Instance != null)
                    {
                    string loadedGamePath = Singleton<LoadManager>.Instance.LoadedGameFolderPath;
                    MelonLogger.Msg($"LoadManager.LoadedGameFolderPath: {loadedGamePath}");

                    if (!string.IsNullOrEmpty(loadedGamePath))
                        {
                        // Look for the Players folder
                        string playersPath = Path.Combine(loadedGamePath, "Players");
                        if (Directory.Exists(playersPath))
                            {
                            string[] playerDirs = Directory.GetDirectories(playersPath);
                            if (playerDirs.Length > 0)
                                {
                                MelonLogger.Msg($"Found player folder: {playerDirs[0]}");
                                return playerDirs[0];
                                }
                            }

                        // If no Players folder, use the game path
                        return loadedGamePath;
                        }
                    }

                // Try SaveManager next
                if (SaveManager.Instance != null)
                    {
                    string playerSavePath = SaveManager.Instance.PlayersSavePath;
                    MelonLogger.Msg($"SaveManager.PlayersSavePath: {playerSavePath}");

                    if (!string.IsNullOrEmpty(playerSavePath) && Directory.Exists(playerSavePath))
                        {
                        return playerSavePath;
                        }
                    }

                // Fallback to standard folder structure
                string baseSavesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
                    "TVGS", "Schedule I", "Saves");

                if (Directory.Exists(baseSavesPath))
                    {
                    MelonLogger.Msg($"Checking save folders in: {baseSavesPath}");
                    string[] steamIdFolders = Directory.GetDirectories(baseSavesPath);

                    if (steamIdFolders.Length > 0)
                        {
                        // Sort by last write time to get most recent
                        Array.Sort(steamIdFolders, (a, b) =>
                            Directory.GetLastWriteTime(b).CompareTo(Directory.GetLastWriteTime(a)));

                        string steamFolder = steamIdFolders[0];
                        MelonLogger.Msg($"Using Steam folder: {steamFolder}");

                        string[] saveGameFolders = Directory.GetDirectories(steamFolder);
                        if (saveGameFolders.Length > 0)
                            {
                            // Sort by last write time
                            Array.Sort(saveGameFolders, (a, b) =>
                                Directory.GetLastWriteTime(b).CompareTo(Directory.GetLastWriteTime(a)));

                            string saveGameFolder = saveGameFolders[0];
                            MelonLogger.Msg($"Using save game folder: {saveGameFolder}");

                            // Check for Players folder
                            string playersFolder = Path.Combine(saveGameFolder, "Players");
                            if (Directory.Exists(playersFolder))
                                {
                                string[] playerFolders = Directory.GetDirectories(playersFolder);
                                if (playerFolders.Length > 0)
                                    {
                                    MelonLogger.Msg($"Using player folder: {playerFolders[0]}");
                                    return playerFolders[0];
                                    }
                                }

                            // If no Players folder, use the save game folder
                            return saveGameFolder;
                            }

                        // If no save game folders, use the Steam ID folder
                        return steamFolder;
                        }

                    // If no Steam ID folders, use the base saves folder
                    return baseSavesPath;
                    }

                // Last resort
                MelonLogger.Warning("No valid save path found, using persistentDataPath");
                return Application.persistentDataPath;
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error getting player folder: {ex.Message}");
                return Application.persistentDataPath;
                }
            }

        private void SaveBackpackData()
            {
            if (backpackStorage == null || backpackStorage.ItemSlots == null)
                {
                MelonLogger.Error("Cannot save backpack - storage is null or has no slots");
                return;
                }

            try
                {
                string playerFolder = GetPlayerFolder();
                string savePath = Path.Combine(playerFolder, BACKPACK_SAVE_FILE + ".json");

                MelonLogger.Msg($"Saving backpack to: {savePath}");

                var saveData = new BackpackSaveData();
                saveData.Items = new List<BackpackItemData>();

                // Go through all item slots and save filled ones
                for (int i = 0; i < backpackStorage.ItemSlots.Count; i++)
                    {
                    var slot = backpackStorage.ItemSlots[i];
                    if (slot != null && slot.ItemInstance != null)
                        {
                        try
                            {
                            saveData.Items.Add(new BackpackItemData
                                {
                                ID = slot.ItemInstance.ID,
                                Quantity = slot.Quantity,
                                SlotIndex = i
                                });
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error processing item in slot {i}: {ex.Message}");
                            }
                        }
                    }

                // Serialize and save data
                string jsonData = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(savePath, jsonData);

                MelonLogger.Msg($"Backpack saved successfully with {saveData.Items.Count} items");
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to save backpack data: {ex.Message}");
                MelonLogger.Error(ex.StackTrace);
                }
            }

        private void LoadBackpackData()
            {
            try
                {
                string playerFolder = GetPlayerFolder();
                string savePath = Path.Combine(playerFolder, BACKPACK_SAVE_FILE + ".json");

                MelonLogger.Msg($"Looking for backpack save at: {savePath}");

                if (!File.Exists(savePath))
                    {
                    MelonLogger.Msg("No backpack save file found, starting with empty backpack");
                    return;
                    }

                // Read and deserialize data
                string jsonData = File.ReadAllText(savePath);
                var saveData = JsonConvert.DeserializeObject<BackpackSaveData>(jsonData);

                MelonLogger.Msg($"Loaded save data with {saveData.Items.Count} items");

                // First clear all slots
                for (int i = 0; i < backpackStorage.ItemSlots.Count; i++)
                    {
                    var slot = backpackStorage.ItemSlots[i];
                    if (slot != null && slot.ItemInstance != null)
                        {
                        slot.ClearStoredInstance(false);
                        }
                    }

                // Then load with a delay
                MelonCoroutines.Start(LoadItemsWithDelay(saveData));
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to load backpack data: {ex.Message}");
                MelonLogger.Error(ex.StackTrace);
                }
            }

        private IEnumerator LoadItemsWithDelay(BackpackSaveData saveData)
            {
            // Wait for slots to be cleared
            yield return new WaitForSeconds(0.5f);

            int loadedItems = 0;

            // Load each item with a delay
            foreach (var itemData in saveData.Items)
                {
                if (itemData.SlotIndex < 0 || itemData.SlotIndex >= backpackStorage.ItemSlots.Count ||
                    string.IsNullOrEmpty(itemData.ID))
                    continue;

                // Wait a frame between each item load
                yield return null;

                try
                    {
                    // Create item JSON for deserialization
                    var itemJson = $"{{\"DataType\":\"ItemData\",\"DataVersion\":0,\"GameVersion\":\"0.3.3f11\",\"ID\":\"{itemData.ID}\",\"Quantity\":{itemData.Quantity}}}";
                    var itemInstance = ItemDeserializer.LoadItem(itemJson);

                    if (itemInstance != null)
                        {
                        var slot = backpackStorage.ItemSlots[itemData.SlotIndex];

                        // First set the instance in the slot
                        slot.SetStoredItem(itemInstance, false);

                        // Then set the quantity
                        slot.SetQuantity(itemData.Quantity, true);

                        loadedItems++;
                        }
                    }
                catch (Exception ex)
                    {
                    MelonLogger.Error($"Error loading item {itemData.ID}: {ex.Message}");
                    }
                }

            MelonLogger.Msg($"Backpack data loaded successfully. {loadedItems} items loaded.");
            }

        public override void OnApplicationQuit()
            {
            SaveBackpackData();
            }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
            {
            if (sceneName == "Main")
                {
                SaveBackpackData();
                }
            }
        }
    }
