using MelonLoader;
using System;
using System.IO;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppScheduleOne.Persistence;
using System.Collections.Generic;
using Il2CppSystem.Globalization;           // IL2CPP version
using Il2CppSystem.Text.RegularExpressions; // IL2CPP version
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppNewtonsoft.Json;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Product.Packaging;
using Il2CppScheduleOne.DevUtilities;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMain), "BackpackMod", "1.2.0", "Tugakit")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod
    {
    public class BackpackMain : MelonMod
        {
        private const int ROWS = 4;
        private const int COLUMNS = 3;
        private const string SAVE_FILENAME = "backpack.json";
        private const KeyCode TOGGLE_KEY = KeyCode.B;
        private const string GAME_VERSION = "0.3.3f12";

        private StorageEntity backpackEntity;
        private bool isInitialized = false;

        // Use Dictionary<string, List<string>> instead of HashSet to avoid IL2Cpp issues
        private Dictionary<string, List<string>> knownPackagingByType = new Dictionary<string, List<string>>();

        public override void OnInitializeMelon()
            {
            MelonLogger.Msg("BackpackMod initialized.");

            // Initialize known packaging types
            knownPackagingByType["Weed"] = new List<string> { "baggie", "jar" };
            knownPackagingByType["Meth"] = new List<string> { "meth" };
            knownPackagingByType["Cocaine"] = new List<string> { "cocaine" };
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
                backpackEntity.StorageEntitySubtitle = "Personal Storage";
                backpackEntity.SlotCount = ROWS * COLUMNS;
                backpackEntity.DisplayRowCount = COLUMNS;
                backpackEntity.MaxAccessDistance = 999f;
                backpackEntity.AccessSettings = StorageEntity.EAccessSettings.Full;

                var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
                for (int i = 0; i < ROWS * COLUMNS; i++)
                    slots.Add(new ItemSlot());
                backpackEntity.ItemSlots = slots;

                backpackEntity.onOpened.AddListener(new Action(() =>
                {
                    MelonLogger.Msg("Backpack opened.");
                    // Discover active packaging types
                    DiscoverPackagingTypes();
                }));
                backpackEntity.onClosed.AddListener(new Action(() =>
                {
                    MelonLogger.Msg("Backpack closed, saving data...");
                    SaveBackpack();
                }));

                LoadBackpack();

                isInitialized = true;
                MelonLogger.Msg($"Backpack setup complete with {ROWS * COLUMNS} slots.");
                }
            catch (Exception ex)
                {
                MelonLogger.Error("Error setting up backpack: " + ex.Message);
                }
            }

        // New method to discover valid packaging types from the game
        private void DiscoverPackagingTypes()
            {
            try
                {
                // Find all storage entities in the scene
                var storageEntities = UnityEngine.Object.FindObjectsOfType<StorageEntity>();
                foreach (var storage in storageEntities)
                    {
                    if (storage == backpackEntity) continue; // Skip our own backpack

                    // Scan each storage for items with packaging
                    for (int i = 0; i < storage.ItemSlots.Count; i++)
                        {
                        var slot = storage.ItemSlots[i];
                        if (slot == null || slot.ItemInstance == null || string.IsNullOrEmpty(slot.ItemInstance.ID))
                            continue;

                        var item = slot.ItemInstance;

                        // Check for product items with packaging
                        if (item is ProductItemInstance productItem && !string.IsNullOrEmpty(productItem.PackagingID))
                            {
                            string drugType = "Unknown";

                            // Determine drug type from the item
                            if (item is WeedInstance) drugType = "Weed";
                            else if (item is MethInstance) drugType = "Meth";
                            else if (item is CocaineInstance) drugType = "Cocaine";
                            else drugType = item.GetType().Name.Replace("Instance", "");

                            // Add to our known packaging types
                            if (!knownPackagingByType.ContainsKey(drugType))
                                knownPackagingByType[drugType] = new List<string>();

                            // Check if we already have this packaging type
                            bool alreadyExists = false;
                            foreach (var pkg in knownPackagingByType[drugType])
                                {
                                if (pkg == productItem.PackagingID)
                                    {
                                    alreadyExists = true;
                                    break;
                                    }
                                }

                            if (!alreadyExists)
                                {
                                knownPackagingByType[drugType].Add(productItem.PackagingID);
                                MelonLogger.Msg($"Discovered packaging: {productItem.PackagingID} for {drugType}");
                                }
                            }
                        }
                    }

                // Log all discovered packaging types
                foreach (var entry in knownPackagingByType)
                    {
                    string packages = string.Join(", ", entry.Value.ToArray());
                    MelonLogger.Msg($"Known packaging for {entry.Key}: {packages}");
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error discovering packaging types: {ex.Message}");
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
                    SaveBackpack();
                    }
                }
            }

        private string GetPlayerFolder()
            {
            try
                {
                // Try to get the loaded game folder path from the game's LoadManager or SaveManager:
                if (Singleton<LoadManager>.Instance != null &&
                    !string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                    {
                    string loadedPath = Singleton<LoadManager>.Instance.LoadedGameFolderPath;
                    string playersFolder = Path.Combine(loadedPath, "Players");
                    if (Directory.Exists(playersFolder))
                        {
                        string[] dirs = Directory.GetDirectories(playersFolder);
                        if (dirs.Length > 0)
                            return dirs[0];
                        }
                    // If no 'Players' subdir, just store in the loaded path.
                    return loadedPath;
                    }

                // If above fails, fallback to SaveManager (in case it stores the path differently).
                if (SaveManager.Instance != null)
                    {
                    string ps = SaveManager.Instance.PlayersSavePath;
                    if (!string.IsNullOrEmpty(ps) && Directory.Exists(ps))
                        return ps;
                    }

                // Otherwise fallback to MelonLoader's default UserData directory.
                return MelonEnvironment.UserDataDirectory;
                }
            catch (Exception ex)
                {
                MelonLogger.Error("Error getting player folder: " + ex.Message);
                return MelonEnvironment.UserDataDirectory;
                }
            }

        private string CreateEmptySlotJson()
            {
            return "{\"DataType\":\"ItemData\",\"DataVersion\":0,\"GameVersion\":\"" + GAME_VERSION + "\",\"ID\":\"\",\"Quantity\":0}";
            }

        private string CreateItemJson(ItemInstance item)
            {
            if (item == null)
                return CreateEmptySlotJson();

            try
                {
                string dataType = DetermineDataType(item);
                string id = item.ID ?? "";
                int quantity = item.Quantity;

                // Base template for all items
                string jsonTemplate = "{{\"DataType\":\"{0}\",\"DataVersion\":0,\"GameVersion\":\"{1}\",\"ID\":\"{2}\",\"Quantity\":{3}{4}}}";
                string extraProps = "";

                // Special handling based on item type
                if (dataType == "CashData")
                    {
                    // Get cash balance
                    float balance = 0;
                    if (item is CashInstance cashItem)
                        {
                        try
                            {
                            balance = cashItem.Balance;
                            MelonLogger.Msg($"Cash item {id} with balance: {balance}");
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error getting cash balance: {ex.Message}");
                            }
                        }

                    // Always include CashBalance for cash items
                    extraProps = string.Format(",\"CashBalance\":{0}", balance);
                    }
                else if (dataType == "WeedData" || dataType == "MethData" || dataType == "CocaineData")
                    {
                    // Get quality and packaging info for drug items
                    string quality = "Standard"; // Default to Standard if we can't get it
                    string packagingID = "";

                    if (item is QualityItemInstance qualityItem)
                        {
                        try
                            {
                            // Get raw enum value as string
                            EQuality enumValue = qualityItem.Quality;
                            quality = enumValue.ToString();

                            // Ensure it's one of the known enum values with correct case
                            // EQuality enum: Trash, Poor, Standard, Premium, Heavenly
                            if (!Enum.IsDefined(typeof(EQuality), quality))
                                {
                                // Try to match case-insensitively
                                foreach (string enumName in Enum.GetNames(typeof(EQuality)))
                                    {
                                    if (string.Equals(enumName, quality, StringComparison.OrdinalIgnoreCase))
                                        {
                                        quality = enumName; // Use the correctly cased version
                                        break;
                                        }
                                    }
                                }

                            MelonLogger.Msg($"Item {id} with quality: {quality}");
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error getting quality: {ex.Message}");
                            }
                        }

                    if (item is ProductItemInstance productItem)
                        {
                        try
                            {
                            packagingID = productItem.PackagingID ?? "";

                            // If packaging is empty, try to assign a valid packaging from discovered types
                            if (string.IsNullOrEmpty(packagingID))
                                {
                                string drugType = dataType.Replace("Data", "");
                                if (knownPackagingByType.ContainsKey(drugType) &&
                                    knownPackagingByType[drugType].Count > 0)
                                    {
                                    // Use the first known packaging for this drug type
                                    packagingID = knownPackagingByType[drugType][0];
                                    MelonLogger.Msg($"Assigned default packaging '{packagingID}' for {drugType}");
                                    }
                                }

                            MelonLogger.Msg($"Product item {id} with packaging: {packagingID}");
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error getting packagingID: {ex.Message}");
                            }
                        }

                    // Always include Quality and PackagingID for drugs
                    extraProps = string.Format(",\"Quality\":\"{0}\",\"PackagingID\":\"{1}\"", quality, packagingID);
                    }
                else if (dataType == "QualityItemData")
                    {
                    // Handle QualityItemData (like liquid meth)
                    string quality = "Standard";
                    if (item is QualityItemInstance qualityItem)
                        {
                        try
                            {
                            // Same quality handling as above
                            EQuality enumValue = qualityItem.Quality;
                            quality = enumValue.ToString();

                            if (!Enum.IsDefined(typeof(EQuality), quality))
                                {
                                foreach (string enumName in Enum.GetNames(typeof(EQuality)))
                                    {
                                    if (string.Equals(enumName, quality, StringComparison.OrdinalIgnoreCase))
                                        {
                                        quality = enumName;
                                        break;
                                        }
                                    }
                                }

                            MelonLogger.Msg($"Quality item {id} with quality: {quality}");
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error getting quality: {ex.Message}");
                            }
                        }

                    // Include only Quality for QualityItemData
                    extraProps = string.Format(",\"Quality\":\"{0}\"", quality);
                    }

                // Format the final JSON string
                return string.Format(jsonTemplate, dataType, GAME_VERSION, id, quantity, extraProps);
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error creating JSON for item {item?.ID}: {ex.Message}");
                return CreateEmptySlotJson();
                }
            }

        private string DetermineDataType(ItemInstance item)
            {
            if (item == null)
                return "ItemData";

            try
                {
                // Type-based detection
                if (item is CashInstance)
                    return "CashData";

                if (item is WeedInstance)
                    return "WeedData";

                if (item is MethInstance)
                    return "MethData";

                if (item is CocaineInstance)
                    return "CocaineData";

                // For other quality items (liquid meth etc)
                if (item is QualityItemInstance && !(item is ProductItemInstance))
                    return "QualityItemData";

                // ID-based checks as fallback
                string itemId = item.ID?.ToLowerInvariant() ?? "";
                if (itemId.Contains("cash") || itemId.Contains("money"))
                    return "CashData";

                if (itemId.Contains("meth") || itemId == "bigpunch")
                    return "MethData";

                if (itemId.Contains("weed") || itemId == "ogkush" || itemId == "greencrack")
                    return "WeedData";

                if (itemId.Contains("cocaine") || itemId.Contains("coke"))
                    return "CocaineData";

                if (itemId.Contains("liquid"))
                    return "QualityItemData";

                // Class name based checks as final fallback
                string typeName = item.GetType().Name;
                if (typeName.Contains("Cash"))
                    return "CashData";

                if (typeName.Contains("Meth"))
                    return "MethData";

                if (typeName.Contains("Weed"))
                    return "WeedData";

                if (typeName.Contains("Cocaine"))
                    return "CocaineData";

                if (typeName.Contains("Quality"))
                    return "QualityItemData";

                if (typeName.EndsWith("Instance"))
                    return typeName.Substring(0, typeName.Length - 8) + "Data";
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error determining data type for {item?.ID}: {ex.Message}");
                }

            return "ItemData";
            }

        private void SaveBackpack()
            {
            if (backpackEntity == null || backpackEntity.ItemSlots == null)
                return;

            try
                {
                string playerFolder = GetPlayerFolder();
                string savePath = Path.Combine(playerFolder, SAVE_FILENAME);

                MelonLogger.Msg("Saving backpack to: " + savePath);

                List<string> slotStrings = new List<string>();
                bool hasCashEntry = false;
                float totalCash = 0f;

                // First pass - collect all items and sum up cash
                for (int i = 0; i < backpackEntity.ItemSlots.Count; i++)
                    {
                    try
                        {
                        var slot = backpackEntity.ItemSlots[i];
                        if (slot != null && slot.ItemInstance != null && !string.IsNullOrEmpty(slot.ItemInstance.ID))
                            {
                            // For cash items, just track the total and skip adding them now
                            if (slot.ItemInstance is CashInstance cashItem)
                                {
                                totalCash += cashItem.Balance;
                                hasCashEntry = true;
                                MelonLogger.Msg($"Slot {i}: Added cash balance {cashItem.Balance} to total");
                                continue;
                                }

                            string itemJson = CreateItemJson(slot.ItemInstance);
                            if (!string.IsNullOrEmpty(itemJson))
                                {
                                slotStrings.Add(itemJson);
                                MelonLogger.Msg($"Slot {i}: Saved item {slot.ItemInstance.ID}");
                                }
                            else
                                {
                                slotStrings.Add(CreateEmptySlotJson());
                                MelonLogger.Msg($"Slot {i}: Empty slot (failed JSON).");
                                }
                            }
                        else
                            {
                            slotStrings.Add(CreateEmptySlotJson());
                            MelonLogger.Msg($"Slot {i}: Empty slot");
                            }
                        }
                    catch (Exception ex)
                        {
                        MelonLogger.Error($"Error saving slot {i}: {ex.Message}");
                        slotStrings.Add(CreateEmptySlotJson());
                        }
                    }

                // Add a single cash entry if we have any cash
                if (hasCashEntry)
                    {
                    string cashJson = $"{{\"DataType\":\"CashData\",\"DataVersion\":0,\"GameVersion\":\"{GAME_VERSION}\",\"ID\":\"cash\",\"Quantity\":1,\"CashBalance\":{totalCash}}}";
                    slotStrings.Add(cashJson);
                    MelonLogger.Msg($"Added consolidated cash entry with balance {totalCash}");
                    }

                // Fill remaining slots with empty items if needed
                while (slotStrings.Count < ROWS * COLUMNS)
                    {
                    slotStrings.Add(CreateEmptySlotJson());
                    }

                // Important: Use "Items" to match the game's format
                JArray array = new JArray();
                foreach (var s in slotStrings)
                    array.Add(s);

                JObject finalObj = new JObject();
                finalObj["Items"] = array;

                string finalJson = finalObj.ToString(Formatting.Indented);
                File.WriteAllText(savePath, finalJson);

                MelonLogger.Msg($"Backpack saved with {slotStrings.Count} items");
                }
            catch (Exception ex)
                {
                MelonLogger.Error("Error saving backpack: " + ex.Message);
                }
            }

        private void LoadBackpack()
            {
            try
                {
                string playerFolder = GetPlayerFolder();
                string savePath = Path.Combine(playerFolder, SAVE_FILENAME);
                if (!File.Exists(savePath))
                    {
                    MelonLogger.Msg("No backpack save file found at: " + savePath);
                    return;
                    }

                MelonLogger.Msg("Loading backpack from: " + savePath);

                string rawText = File.ReadAllText(savePath);

                // Fix decimal separator issues - handles both comma and period decimal separators
                rawText = Regex.Replace(rawText, "\"CashBalance\":(\\d+)[,\\.](\\d+)", "\"CashBalance\":$1.$2");

                JObject root = JObject.Parse(rawText);
                JToken token = root["Items"];
                if (token == null)
                    {
                    MelonLogger.Error("No 'Items' array found in the JSON!");
                    return;
                    }

                if (token.Type != JTokenType.Array)
                    {
                    MelonLogger.Error($"'Items' is not a JSON array! Type={token.Type}");
                    return;
                    }

                JArray itemsArray = (JArray)token;

                // Clear existing
                for (int i = 0; i < backpackEntity.ItemSlots.Count; i++)
                    {
                    var slot = backpackEntity.ItemSlots[i];
                    if (slot != null)
                        slot.ItemInstance = null;
                    }

                int loadedCount = 0;

                // First, load all non-cash items
                List<string> cashJsonItems = new List<string>();
                int itemCount = itemsArray.Count;
                for (int i = 0; i < Math.Min(itemCount, backpackEntity.ItemSlots.Count); i++)
                    {
                    JToken itemToken = itemsArray[i];
                    string rawSlotJson = itemToken.ToString();

                    // Check if it's a cash item
                    if (rawSlotJson.Contains("\"DataType\":\"CashData\""))
                        {
                        cashJsonItems.Add(rawSlotJson);
                        continue;
                        }

                    if (LoadItemIntoSlot(rawSlotJson, i))
                        loadedCount++;
                    }

                // If we have cash items, consolidate them
                if (cashJsonItems.Count > 0)
                    {
                    float totalCash = 0f;
                    foreach (string cashJson in cashJsonItems)
                        {
                        try
                            {
                            // Parse the cash item to get the balance
                            JObject cashObj = JObject.Parse(cashJson);
                            JToken balanceToken = cashObj["CashBalance"];
                            float balance = 0f;
                            if (balanceToken != null)
                                {
                                balance = float.Parse(balanceToken.ToString());
                                }
                            totalCash += balance;
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error parsing cash item: {ex.Message}");
                            }
                        }

                    // Find an empty slot for the consolidated cash
                    for (int i = 0; i < backpackEntity.ItemSlots.Count; i++)
                        {
                        var slot = backpackEntity.ItemSlots[i];
                        if (slot != null && slot.ItemInstance == null)
                            {
                            // Create a new consolidated cash JSON
                            string consolidatedCashJson = $"{{\"DataType\":\"CashData\",\"DataVersion\":0,\"GameVersion\":\"{GAME_VERSION}\",\"ID\":\"cash\",\"Quantity\":1,\"CashBalance\":{totalCash}}}";

                            if (LoadItemIntoSlot(consolidatedCashJson, i))
                                {
                                loadedCount++;
                                MelonLogger.Msg($"Loaded consolidated cash with balance {totalCash}");
                                }
                            break;
                            }
                        }
                    }

                MelonLogger.Msg($"Loaded {loadedCount} items from backpack save.");
                }
            catch (Exception ex)
                {
                MelonLogger.Error("Error loading backpack: " + ex.Message);
                }
            }

        private bool LoadItemIntoSlot(string rawJson, int slotIndex)
            {
            if (string.IsNullOrEmpty(rawJson) || slotIndex < 0 || slotIndex >= backpackEntity.ItemSlots.Count)
                return false;

            var slot = backpackEntity.ItemSlots[slotIndex];
            if (slot == null)
                return false;

            try
                {
                var itemInstance = ItemDeserializer.LoadItem(rawJson);
                if (itemInstance != null)
                    {
                    // Debug the loaded item
                    if (itemInstance is CashInstance cash)
                        MelonLogger.Msg($"Loaded cash with balance: {cash.Balance}");

                    if (itemInstance is QualityItemInstance quality)
                        MelonLogger.Msg($"Loaded quality item with quality: {quality.Quality}");

                    if (itemInstance is ProductItemInstance product)
                        MelonLogger.Msg($"Loaded product with packaging: {product.PackagingID ?? "(empty)"}");

                    slot.ItemInstance = itemInstance;
                    MelonLogger.Msg($"Loaded item {itemInstance.ID} into slot {slotIndex}");
                    return true;
                    }

                MelonLogger.Warning($"Failed to load item: {rawJson}");
                return false;
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error loading item into slot {slotIndex}: {ex.Message}");
                return false;
                }
            }
        }
    }
