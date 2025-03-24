using FishNet.Connection;
using FishNet.Object;
using ScheduleOne.ItemFramework;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Items;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using Newtonsoft.Json;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.Persistence;
using ScheduleOne;

namespace Backpack
    {
    [Serializable]
    public class BackpackSaveData
        {
        public List<SerializedItemSlot> Slots = new List<SerializedItemSlot>();
        }

    [Serializable]
    public class SerializedItemSlot
        {
        public string ItemID = "";
        public int Quantity = 0;
        public Dictionary<string, string> CustomData = new Dictionary<string, string>();
        }

    public class BackpackSaver : MonoBehaviour, ISaveable
        {
        private BackpackContainer container;

        // ISaveable Properties
        public string SaveFolderName => "Backpack";
        public string SaveFileName => "Backpack";
        public Loader Loader => null;
        public bool ShouldSaveUnderFolder => false;
        public List<string> LocalExtraFiles { get; set; } = new List<string>();
        public List<string> LocalExtraFolders { get; set; } = new List<string>();
        public bool HasChanged { get; set; } = true;

        public void Initialize(BackpackContainer backpackContainer)
            {
            try
                {
                container = backpackContainer;

                if (Singleton<SaveManager>.Instance != null)
                    {
                    Singleton<SaveManager>.Instance.RegisterSaveable(this);
                    MelonLogger.Msg("BackpackSaver registered with SaveManager");
                    }
                else
                    {
                    MelonLogger.Error("SaveManager not found, backpack data will be saved separately");
                    }

                LocalExtraFiles = new List<string>();
                LocalExtraFolders = new List<string>();
                HasChanged = true;

                // Start delayed loading process
                MelonCoroutines.Start(DelayedLoad());
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to initialize BackpackSaver: {ex.Message}");
                }
            }

        private IEnumerator DelayedLoad()
            {
            // Attend plus longtemps pour s'assurer que tout est initialisé
            for (int i = 0; i < 30; i++)
                yield return null;

            // Attend que le LoadManager soit prêt
            while (Singleton<LoadManager>.Instance == null)
                yield return new WaitForSeconds(0.1f);

            // Attend d'avoir un chemin valide
            int attempts = 0;
            while (string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                {
                yield return new WaitForSeconds(0.1f);
                attempts++;
                if (attempts > 50) // 5 secondes max d'attente
                    {
                    MelonLogger.Warning("Délai dépassé en attendant le chemin du LoadManager");
                    yield break;
                    }
                }

            // Force une attente supplémentaire après que le chemin est disponible
            yield return new WaitForSeconds(1.0f);

            string path = Singleton<LoadManager>.Instance.LoadedGameFolderPath;
            MelonLogger.Msg($"Tentative de charger les données du backpack depuis: {path}");

            // Toujours essayer de charger, même après un retour de menu
            Load(path);
            }

        private void Load(string folderPath)
            {
            try
                {
                if (container == null || container.ItemSlots == null)
                    {
                    MelonLogger.Error("Cannot load backpack - container is null or has no slots");
                    return;
                    }

                string filePath = Path.Combine(folderPath, SaveFileName + ".json");
                MelonLogger.Msg($"Looking for backpack save at: {filePath}");

                if (!File.Exists(filePath))
                    {
                    MelonLogger.Msg("No backpack save data found at " + filePath);
                    return;
                    }

                string json = File.ReadAllText(filePath);
                MelonLogger.Msg($"Loaded backpack JSON data, length: {json.Length}");

                BackpackSaveData saveData = null;

                try
                    {
                    saveData = JsonConvert.DeserializeObject<BackpackSaveData>(json);
                    }
                catch (Exception ex)
                    {
                    MelonLogger.Warning($"Failed to deserialize backpack save data: {ex.Message}");
                    return;
                    }

                if (saveData == null || saveData.Slots == null)
                    {
                    MelonLogger.Warning("Invalid backpack save data structure");
                    return;
                    }

                MelonLogger.Msg($"Parsed save data with {saveData.Slots.Count} slots");

                // Important: Vidons tous les slots AVANT de charger
                for (int i = 0; i < container.ItemSlots.Count; i++)
                    {
                    var slot = container.ItemSlots[i];
                    if (slot != null && slot.ItemInstance != null)
                        {
                        slot.ClearStoredInstance(true);
                        }
                    }

                // Donnons un peu de temps pour que le nettoyage prenne effet
                MelonCoroutines.Start(LoadItemsWithDelay(saveData));
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to load backpack data: {ex.Message}\n{ex.StackTrace}");
                }
            }

        private IEnumerator LoadItemsWithDelay(BackpackSaveData saveData)
            {
            // Attendre que le nettoyage des slots soit terminé
            yield return new WaitForSeconds(0.5f);

            int loadedItems = 0;

            // Charger chaque item avec un délai
            for (int i = 0; i < Math.Min(saveData.Slots.Count, container.ItemSlots.Count); i++)
                {
                    var serializedSlot = saveData.Slots[i];
                    var slot = container.ItemSlots[i];

                    if (slot == null || string.IsNullOrEmpty(serializedSlot.ItemID))
                        continue;

                    // Attendre une frame entre chaque chargement d'item
                    yield return null;

                    var itemDef = Registry.GetItem(serializedSlot.ItemID);
                    if (itemDef != null)
                        {
                        var instance = itemDef.GetDefaultInstance();

                        // D'abord définir l'instance dans le slot (SANS triggerEvents pour éviter des conflits)
                        slot.SetStoredItem(instance, false);

                        // Attendre une frame
                        yield return null;

                        // Puis définir la quantité (AVEC triggerEvents pour déclencher les mises à jour)
                        slot.SetQuantity(serializedSlot.Quantity, true);

                        // Forcer un événement de mise à jour
                        if (instance.onDataChanged != null)
                            {
                            instance.onDataChanged();
                            }

                        // Attendre une autre frame
                        yield return null;

                        loadedItems++;
                        }
                    }
                

            MelonLogger.Msg($"Backpack data loaded successfully. {loadedItems} items loaded.");

            // Force une mise à jour finale après un délai
            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < container.ItemSlots.Count; i++)
                {
                var slot = container.ItemSlots[i];
                if (slot?.ItemInstance != null && slot.ItemInstance.onDataChanged != null)
                    {
                    slot.ItemInstance.onDataChanged();
                    }
                }
            }

        public string GetSaveString()
            {
            try
                {
                if (container == null)
                    {
                    MelonLogger.Error("Cannot save backpack - container is null");
                    return null;
                    }

                if (container.ItemSlots == null)
                    {
                    MelonLogger.Error("Cannot save backpack - container.ItemSlots is null");
                    return null;
                    }

                BackpackSaveData saveData = new BackpackSaveData();
                saveData.Slots = new List<SerializedItemSlot>();

                foreach (var slot in container.ItemSlots)
                    {
                    if (slot == null)
                        {
                        saveData.Slots.Add(new SerializedItemSlot());
                        continue;
                        }

                    SerializedItemSlot serializedSlot = new SerializedItemSlot();

                    if (slot.ItemInstance != null)
                        {
                        try
                            {
                            serializedSlot.ItemID = slot.ItemInstance.ID ?? "";
                            serializedSlot.Quantity = slot.Quantity;
                            serializedSlot.Quantity = slot.Quantity;
                            }
                        catch (Exception ex)
                            {
                            MelonLogger.Error($"Error accessing item data: {ex.Message}");
                            }
                        }

                    saveData.Slots.Add(serializedSlot);
                    }

                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                return json;
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to get save string for backpack data: {ex.Message}");
                return null;
                }
            }

        public string Save(string folderPath)
            {
            try
                {
                string saveData = GetSaveString();
                if (string.IsNullOrEmpty(saveData))
                    {
                    MelonLogger.Error("Failed to save - save string is empty");
                    return null;
                    }

                string filePath = Path.Combine(folderPath, SaveFileName + ".json");
                File.WriteAllText(filePath, saveData);
                MelonLogger.Msg($"Backpack data saved to {filePath}");
                HasChanged = false;
                return SaveFileName;
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to save backpack data: {ex.Message}");
                return null;
                }
            }

        // ISaveable interface implementation
        public void InitializeSaveable() { }
        public List<string> WriteData(string parentFolderPath) => new List<string>();
        public void DeleteUnapprovedFiles(string parentFolderPath) { }

        public string GetContainerFolder(string parentFolderPath)
            {
            string path = Path.Combine(parentFolderPath, SaveFolderName);
            if (!Directory.Exists(path))
                {
                try { Directory.CreateDirectory(path); }
                catch { }
                }
            return path;
            }

        public string GetLocalPath(out bool isFolder)
            {
            isFolder = ShouldSaveUnderFolder;
            return ShouldSaveUnderFolder ? SaveFolderName : SaveFileName + ".json";
            }

        public void CompleteSave(string parentFolderPath, bool writeDataFile)
            {
            HasChanged = false;
            }
        public bool TryLoadFile(string path, out string contents, bool autoAddExtension = true)
            {
            contents = string.Empty;
            try
                {
                string fullPath = autoAddExtension ? path + ".json" : path;
                if (!File.Exists(fullPath)) return false;
                contents = File.ReadAllText(fullPath);
                return true;
                }
            catch
                {
                return false;
                }
            }

        public bool TryLoadFile(string parentPath, string fileName, out string contents)
            {
            return TryLoadFile(Path.Combine(parentPath, fileName), out contents);
            }

        public string WriteFolder(string parentPath, string localPath_NoExtensions)
            {
            try
                {
                bool isFolder;
                string str = Path.Combine(parentPath, GetLocalPath(out isFolder));
                if (!isFolder) return string.Empty;

                if (!Directory.Exists(str)) return string.Empty;

                if (localPath_NoExtensions.Contains(".json")) return string.Empty;

                string path = Path.Combine(parentPath, str, localPath_NoExtensions);
                Directory.CreateDirectory(path);
                return localPath_NoExtensions;
                }
            catch
                {
                return string.Empty;
                }
            }

        public string WriteSubfile(string parentPath, string localPath_NoExtensions, string contents)
            {
            try
                {
                bool isFolder;
                string str = Path.Combine(parentPath, GetLocalPath(out isFolder));
                if (!isFolder || !Directory.Exists(str) || localPath_NoExtensions.Contains(".json"))
                    return string.Empty;

                string path3 = localPath_NoExtensions + ".json";
                string path = Path.Combine(parentPath, str, path3);
                File.WriteAllText(path, contents);
                return path3;
                }
            catch
                {
                return string.Empty;
                }
            }

        public void WriteBaseData(string parentFolderPath, string saveString)
            {
            if (string.IsNullOrEmpty(saveString)) return;

            try
                {
                string path = Path.Combine(parentFolderPath, SaveFileName + ".json");
                if (ShouldSaveUnderFolder)
                    path = Path.Combine(GetContainerFolder(parentFolderPath), SaveFileName + ".json");

                File.WriteAllText(path, saveString);
                CompleteSave(parentFolderPath, true);
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to write save data: {ex.Message}");
                }
            }

        public void ReadFolder(string folderPath, string fileName) => Load(folderPath);
        public bool LoadingComplete() => true;
        public string GetSaveName() => SaveFileName;
        }

    public class BackpackContainer : MonoBehaviour, IItemSlotOwner
        {
        public List<ItemSlot> ItemSlots { get; set; } = new List<ItemSlot>();

        private void Awake()
            {
            for (int i = 0; i < 12; i++)
                ItemSlots.Add(new ItemSlot());

            foreach (var slot in ItemSlots)
                slot.SetSlotOwner(this);
            }

        public void SetStoredInstance(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
            {
            if (itemSlotIndex < 0 || itemSlotIndex >= ItemSlots.Count) return;
            ItemSlots[itemSlotIndex].SetStoredItem(instance, true);
            }

        public void SetItemSlotQuantity(int itemSlotIndex, int quantity)
            {
            if (itemSlotIndex < 0 || itemSlotIndex >= ItemSlots.Count) return;
            ItemSlots[itemSlotIndex].SetQuantity(quantity, true);
            }

        public void SetSlotLocked(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
            {
            if (itemSlotIndex < 0 || itemSlotIndex >= ItemSlots.Count) return;

            if (locked)
                ItemSlots[itemSlotIndex].ApplyLock(lockOwner, lockReason, true);
            else
                ItemSlots[itemSlotIndex].RemoveLock(true);
            }

        public void SendItemsToClient(NetworkConnection conn)
            {
            for (int i = 0; i < ItemSlots.Count; i++)
                {
                if (ItemSlots[i].IsLocked)
                    SetSlotLocked(conn, i, true, ItemSlots[i].ActiveLock.LockOwner, ItemSlots[i].ActiveLock.LockReason);

                if (ItemSlots[i].ItemInstance != null)
                    SetStoredInstance(conn, i, ItemSlots[i].ItemInstance);
                }
            }

        public int GetTotalItemCount()
            {
            int count = 0;
            foreach (var slot in ItemSlots)
                if (slot?.ItemInstance != null)
                    count += slot.Quantity;
            return count;
            }

        public int GetItemCount(string id)
            {
            int count = 0;
            foreach (var slot in ItemSlots)
                if (slot?.ItemInstance != null && slot.ItemInstance.ID == id)
                    count += slot.Quantity;
            return count;
            }
        }

    public class BackpackSlotClickHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
        public ItemSlotUI SlotUI { get; private set; }
        public ItemSlot TargetSlot { get; private set; }
        public BackpackMod Mod { get; private set; }
        private bool isDragging = false;

        public void Initialize(ItemSlotUI slotUI, ItemSlot targetSlot, BackpackMod mod)
            {
            SlotUI = slotUI;
            TargetSlot = targetSlot;
            Mod = mod;
            }

        public void OnPointerClick(PointerEventData eventData)
            {
            try
                {
                // Skip if conditions aren't met
                if (isDragging || SlotUI == null || SlotUI.assignedSlot == null ||
                    SlotUI.assignedSlot.ItemInstance == null || Mod == null)
                    return;

                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!shiftPressed) return;

                ItemSlot sourceSlot = SlotUI.assignedSlot;

                // Skip if slot is locked
                if (sourceSlot.IsLocked || sourceSlot.IsRemovalLocked)
                    return;

                // Get target slots based on source
                List<ItemSlot> targetSlots;
                if (Mod.Container?.ItemSlots != null && Mod.Container.ItemSlots.Contains(sourceSlot))
                    targetSlots = Mod.hotbarSlots; // From backpack to hotbar
                else
                    targetSlots = Mod.Container?.ItemSlots; // From hotbar to backpack

                if (targetSlots == null || targetSlots.Count == 0)
                    return;

                // Process based on button
                if (eventData.button == PointerEventData.InputButton.Left)
                    TransferStack(sourceSlot, targetSlots);
                else if (eventData.button == PointerEventData.InputButton.Right)
                    TransferSingleItem(sourceSlot, targetSlots);
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error in OnPointerClick: {ex.Message}");
                }
            }

        private void TransferStack(ItemSlot sourceSlot, List<ItemSlot> targetSlots)
            {
            try
                {
                if (sourceSlot?.ItemInstance == null || sourceSlot.Quantity <= 0)
                    return;

                int quantityToMove = sourceSlot.Quantity;
                int quantityMoved = 0;

                // First try to add to existing stacks
                foreach (var targetSlot in targetSlots)
                    {
                    if (targetSlot == null || targetSlot.IsLocked || targetSlot.IsAddLocked ||
                        !targetSlot.DoesItemMatchFilters(sourceSlot.ItemInstance))
                        continue;

                    if (targetSlot.ItemInstance != null &&
                        targetSlot.ItemInstance.CanStackWith(sourceSlot.ItemInstance, false) &&
                        targetSlot.Quantity < targetSlot.ItemInstance.StackLimit)
                        {
                        int spaceInSlot = targetSlot.ItemInstance.StackLimit - targetSlot.Quantity;
                        int amountToMove = Mathf.Min(spaceInSlot, quantityToMove - quantityMoved);

                        if (amountToMove > 0)
                            {
                            targetSlot.ChangeQuantity(amountToMove);
                            quantityMoved += amountToMove;

                            if (quantityMoved >= quantityToMove)
                                break;
                            }
                        }
                    }

                // Then try empty slots if needed
                if (quantityMoved < quantityToMove)
                    {
                    foreach (var targetSlot in targetSlots)
                        {
                        if (targetSlot == null || targetSlot.IsLocked || targetSlot.IsAddLocked ||
                            !targetSlot.DoesItemMatchFilters(sourceSlot.ItemInstance) ||
                            targetSlot.ItemInstance != null)
                            continue;

                        int amountToMove = quantityToMove - quantityMoved;
                        ItemInstance newInstance = sourceSlot.ItemInstance.GetCopy(amountToMove);
                        if (newInstance != null)
                            {
                            targetSlot.SetStoredItem(newInstance);
                            quantityMoved += amountToMove;
                            break; // One slot is enough
                            }
                        }
                    }

                // Reduce source quantity
                if (quantityMoved > 0)
                    {
                    if (quantityMoved >= sourceSlot.Quantity)
                        sourceSlot.ClearStoredInstance();
                    else
                        sourceSlot.ChangeQuantity(-quantityMoved);

                    // Save changes
                    Mod?.SaveAfterItemMove();
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error in TransferStack: {ex.Message}");
                }
            }

        private void TransferSingleItem(ItemSlot sourceSlot, List<ItemSlot> targetSlots)
            {
            try
                {
                if (sourceSlot?.ItemInstance == null || sourceSlot.Quantity <= 0)
                    return;

                // Try to add to existing stack first
                foreach (var targetSlot in targetSlots)
                    {
                    if (targetSlot == null || targetSlot.IsLocked || targetSlot.IsAddLocked ||
                        !targetSlot.DoesItemMatchFilters(sourceSlot.ItemInstance))
                        continue;

                    if (targetSlot.ItemInstance != null &&
                        targetSlot.ItemInstance.CanStackWith(sourceSlot.ItemInstance, false) &&
                        targetSlot.Quantity < targetSlot.ItemInstance.StackLimit)
                        {
                        targetSlot.ChangeQuantity(1);
                        sourceSlot.ChangeQuantity(-1);
                        Mod?.SaveAfterItemMove();
                        return;
                        }
                    }

                // If no suitable stack, try empty slot
                foreach (var targetSlot in targetSlots)
                    {
                    if (targetSlot == null || targetSlot.IsLocked || targetSlot.IsAddLocked ||
                        !targetSlot.DoesItemMatchFilters(sourceSlot.ItemInstance) ||
                        targetSlot.ItemInstance != null)
                        continue;

                    ItemInstance newInstance = sourceSlot.ItemInstance.GetCopy(1);
                    if (newInstance != null)
                        {
                        targetSlot.SetStoredItem(newInstance);
                        sourceSlot.ChangeQuantity(-1);
                        Mod?.SaveAfterItemMove();
                        return;
                        }
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error in TransferSingleItem: {ex.Message}");
                }
            }

        // Required interface methods
        public void OnBeginDrag(PointerEventData eventData) => isDragging = true;
        public void OnDrag(PointerEventData eventData) { } // Required but empty
        public void OnEndDrag(PointerEventData eventData)
            {
            isDragging = false;

            // Ensure we save after drag operations
            if (Mod != null)
                {
                Mod.SaveAfterItemMove();
                }
            }
        }

    public class BackpackMod : MelonMod
        {
        private static BackpackMod instance;
        private GameObject backpackUI;
        private List<ItemSlotUI> slots = new List<ItemSlotUI>();
        private bool isOpen;
        private BackpackContainer container;
        private BackpackSaver saver;
        public List<ItemSlot> hotbarSlots = new List<ItemSlot>();
        private bool raycasterRegistered = false;
        private bool wasHotbarEnabled = true;
        private bool wasEquippingEnabled = true;
        private string backpackUIElementName = "BackpackUI";
        private List<BackpackSlotClickHandler> clickHandlers = new List<BackpackSlotClickHandler>();
        private bool uiInitialized = false;

        public static BackpackMod Instance => instance;
        public BackpackContainer Container => container;

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
            {
            if (sceneName == "Main")
                {
                // Sauvegarder avant de quitter la scène
                if (saver != null && Singleton<LoadManager>.Instance != null &&
                    !string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                    {
                    saver.Save(Singleton<LoadManager>.Instance.LoadedGameFolderPath);
                    }

                // Détruire complètement le backpack UI
                if (backpackUI != null)
                    {
                    UnityEngine.Object.Destroy(backpackUI);
                    backpackUI = null;
                    container = null;
                    saver = null;
                    slots.Clear();
                    clickHandlers.Clear();
                    hotbarSlots.Clear();
                    uiInitialized = false;
                    raycasterRegistered = false;
                    }

                MelonLogger.Msg("Backpack UI completely destroyed when leaving Main scene");
                }
            }

        public override void OnApplicationStart()
            {
            instance = this;
            MelonLogger.Msg("BackpackMod loaded");
            }

        public override void OnApplicationQuit()
            {
            try
                {
                if (saver != null && Singleton<LoadManager>.Instance != null &&
                    !string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                    {
                    saver.Save(Singleton<LoadManager>.Instance.LoadedGameFolderPath);
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error saving on quit: {ex.Message}");
                }
            }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
            {
            if (sceneName == "Main")
                {
                MelonLogger.Msg("Main scene loaded, creating new backpack UI");

                // Toujours créer une nouvelle UI
                CreateBackpackUI();
                }
            }

        private void CreateBackpackUI()
            {
            // Nettoyer tout vestige précédent
            if (backpackUI != null)
                {
                UnityEngine.Object.Destroy(backpackUI);
                backpackUI = null;
                }

            // Réinitialiser les collections
            slots.Clear();
            clickHandlers.Clear();
            uiInitialized = false;
            raycasterRegistered = false;

            // Créer une nouvelle UI
            MelonCoroutines.Start(CreateBackpackUICoroutine());
            }

        private IEnumerator CreateBackpackUICoroutine()
            {
            MelonLogger.Msg("Starting CreateBackpackUICoroutine");

            // Wait for ItemUIManager
            while (Singleton<ItemUIManager>.Instance == null)
                yield return null;

            ItemUIManager itemUIManager = Singleton<ItemUIManager>.Instance;

            // Wait for ItemSlotUIPrefab
            while (itemUIManager.ItemSlotUIPrefab == null)
                yield return null;

            // Create main UI
            backpackUI = new GameObject("BackpackUI");
            Canvas canvas = backpackUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            backpackUI.AddComponent<ScheduleOne.UI.CanvasScaler>();
            GraphicRaycaster raycaster = backpackUI.AddComponent<GraphicRaycaster>();
            container = backpackUI.AddComponent<BackpackContainer>();

            // Wait a frame for container to initialize
            yield return null;

            // Create and initialize saver
            saver = backpackUI.AddComponent<BackpackSaver>();

            // Register raycaster
            RegisterRaycaster(raycaster);

            // Create slots container
            GameObject slotsContainer = new GameObject("SlotsContainer");
            slotsContainer.transform.SetParent(backpackUI.transform, false);
            RectTransform slotContainerRect = slotsContainer.AddComponent<RectTransform>();
            slotContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotContainerRect.pivot = new Vector2(0.5f, 0.5f);
            slotContainerRect.anchoredPosition = Vector2.zero;

            // Configure grid layout
            GridLayoutGroup gridLayoutGroup = slotsContainer.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.cellSize = new Vector2(100f, 100f);
            gridLayoutGroup.spacing = new Vector2(10f, 10f);
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = 4;

            // Calculate container size
            float width = 4f * gridLayoutGroup.cellSize.x + 3f * gridLayoutGroup.spacing.x;
            float height = 3f * gridLayoutGroup.cellSize.y + 2f * gridLayoutGroup.spacing.y;
            slotContainerRect.sizeDelta = new Vector2(width, height);

            // Clear lists
            slots.Clear();
            clickHandlers.Clear();

            // Create slots
            for (int i = 0; i < 12; i++)
                {
                yield return null; // Spread creation over frames

                ItemSlotUI slotInstance = UnityEngine.Object.Instantiate(
                    itemUIManager.ItemSlotUIPrefab, slotsContainer.transform);

                if (slotInstance == null) continue;

                slotInstance.name = $"Slot_{i}";
                slotInstance.GetComponent<RectTransform>().localScale = Vector3.one;

                if (i < container.ItemSlots.Count)
                    slotInstance.AssignSlot(container.ItemSlots[i]);

                BackpackSlotClickHandler clickHandler = slotInstance.gameObject.AddComponent<BackpackSlotClickHandler>();
                if (clickHandler != null && i < container.ItemSlots.Count)
                    {
                    clickHandler.Initialize(slotInstance, container.ItemSlots[i], this);
                    clickHandlers.Add(clickHandler);
                    }

                slots.Add(slotInstance);
                }

            try
                {
                // Hide UI and set as persistent
                backpackUI.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(backpackUI);

                // Find hotbar slots
                hotbarSlots.Clear();
                FindHotbarSlots();

                uiInitialized = true;
                MelonLogger.Msg("Backpack UI creation complete");

                // Initialize the saver after everything else is set up
                saver.Initialize(container);
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error creating backpack UI: {ex.Message}");
                }
            }

        private void RegisterRaycaster(GraphicRaycaster raycaster)
            {
            try
                {
                if (raycaster == null || raycasterRegistered) return;

                ItemUIManager instance = Singleton<ItemUIManager>.Instance;
                if (instance == null || instance.Raycasters == null) return;

                GraphicRaycaster[] newArray = new GraphicRaycaster[instance.Raycasters.Length + 1];
                Array.Copy(instance.Raycasters, newArray, instance.Raycasters.Length);
                newArray[instance.Raycasters.Length] = raycaster;
                instance.Raycasters = newArray;
                raycasterRegistered = true;

                MelonLogger.Msg("Backpack: Raycaster registered with ItemUIManager");
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Failed to register raycaster: {ex.Message}");
                }
            }

        private void FindHotbarSlots()
            {
            MelonCoroutines.Start(FindHotbarSlotsCoroutine());
            }

        private IEnumerator FindHotbarSlotsCoroutine()
            {
            yield return null;
            yield return null;

            PlayerInventory playerInventory = PlayerSingleton<PlayerInventory>.Instance;

            if (playerInventory != null && playerInventory.hotbarSlots != null && playerInventory.hotbarSlots.Count > 0)
                {
                foreach (var slot in playerInventory.hotbarSlots)
                    {
                    if (slot != null)
                        hotbarSlots.Add((ItemSlot)slot);
                    }

                if (hotbarSlots.Count > 0)
                    {
                    MelonLogger.Msg($"Backpack: Found {hotbarSlots.Count} hotbar slots from PlayerInventory");
                    }
                else
                    {
                    MelonLogger.Msg("Backpack: No hotbar slots found in PlayerInventory, searching UI...");
                    FindHotbarSlotsByUI();
                    }
                }
            else
                {
                MelonLogger.Msg("Backpack: PlayerInventory not found or hotbarSlots is null, searching UI...");
                FindHotbarSlotsByUI();
                }

            yield return null;
            MelonCoroutines.Start(SetupHotbarClickHandlers());
            }

        private void FindHotbarSlotsByUI()
            {
            try
                {
                foreach (ItemSlotUI itemSlotUi in UnityEngine.Object.FindObjectsOfType<ItemSlotUI>())
                    {
                    if (itemSlotUi != null && itemSlotUi.assignedSlot != null && !slots.Contains(itemSlotUi) &&
                        (itemSlotUi.assignedSlot is HotbarSlot ||
                         itemSlotUi.name.Contains("Hotbar") ||
                         (itemSlotUi.transform.parent != null && itemSlotUi.transform.parent.name.Contains("Hotbar"))))
                        {
                        hotbarSlots.Add(itemSlotUi.assignedSlot);
                        }
                    }

                MelonLogger.Msg($"Backpack: Found {hotbarSlots.Count} hotbar slots by UI search");
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error finding hotbar slots: {ex.Message}");
                }
            }

        private IEnumerator SetupHotbarClickHandlers()
            {
            yield return null;

            try
                {
                ItemSlotUI[] itemSlotUiArray = UnityEngine.Object.FindObjectsOfType<ItemSlotUI>();

                foreach (var slotUI in itemSlotUiArray)
                    {
                    if (slotUI != null && slotUI.assignedSlot != null && !slots.Contains(slotUI) &&
                        hotbarSlots.Contains(slotUI.assignedSlot))
                        {
                        BackpackSlotClickHandler clickHandler = slotUI.gameObject.AddComponent<BackpackSlotClickHandler>();
                        if (clickHandler != null)
                            {
                            clickHandler.Initialize(slotUI, slotUI.assignedSlot, this);
                            clickHandlers.Add(clickHandler);
                            }
                        }
                    }

                MelonLogger.Msg($"Backpack: Added click handlers to {clickHandlers.Count} slots");
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error setting up hotbar click handlers: {ex.Message}");
                }
            }

        public override void OnUpdate()
            {
            if (Input.GetKeyDown(KeyCode.B))
                ToggleBackpack();
            }

        private void ToggleBackpack()
            {
            try
                {
                if (!uiInitialized || backpackUI == null)
                    {
                    MelonLogger.Error("Cannot toggle backpack: UI not initialized");
                    return;
                    }

                isOpen = !isOpen;
                backpackUI.SetActive(isOpen);

                if (Singleton<ItemUIManager>.Instance != null)
                    {
                    ItemUIManager instance = Singleton<ItemUIManager>.Instance;

                    if (!raycasterRegistered)
                        {
                        GraphicRaycaster raycaster = backpackUI.GetComponent<GraphicRaycaster>();
                        if (raycaster != null) RegisterRaycaster(raycaster);
                        }

                    instance.SetDraggingEnabled(isOpen);

                    if (isOpen && hotbarSlots != null && hotbarSlots.Count > 0)
                        {
                        List<ItemSlot> primarySlots = new List<ItemSlot>();

                        foreach (ItemSlotUI slot in slots)
                            {
                            if (slot?.assignedSlot != null)
                                primarySlots.Add(slot.assignedSlot);
                            }

                        if (primarySlots.Count > 0)
                            instance.EnableQuickMove(primarySlots, hotbarSlots);
                        }
                    else if (!isOpen)
                        {
                        // Save on close
                        if (saver != null && Singleton<LoadManager>.Instance != null &&
                            !string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                            {
                            saver.Save(Singleton<LoadManager>.Instance.LoadedGameFolderPath);
                            }
                        }
                    }

                ControlPlayerInteraction(isOpen);
                Cursor.visible = isOpen;
                Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error toggling backpack: {ex.Message}");
                }
            }

        public void SaveAfterItemMove()
            {
            MelonCoroutines.Start(DelayedSave());
            }

        private IEnumerator DelayedSave()
            {
            yield return new WaitForSeconds(0.2f);

            try
                {
                if (saver != null && Singleton<LoadManager>.Instance != null &&
                    !string.IsNullOrEmpty(Singleton<LoadManager>.Instance.LoadedGameFolderPath))
                    {
                    saver.Save(Singleton<LoadManager>.Instance.LoadedGameFolderPath);
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error in delayed save: {ex.Message}");
                }
            }

        private void ControlPlayerInteraction(bool disableInteraction)
            {
            try
                {
                // Control camera
                PlayerCamera cameraInstance = PlayerSingleton<PlayerCamera>.Instance;
                if (cameraInstance != null)
                    {
                    if (disableInteraction)
                        {
                        cameraInstance.SetCanLook(false);
                        cameraInstance.AddActiveUIElement(backpackUIElementName);
                        }
                    else
                        {
                        cameraInstance.SetCanLook(true);
                        cameraInstance.RemoveActiveUIElement(backpackUIElementName);
                        }
                    }

                // Control inventory
                PlayerInventory inventoryInstance = PlayerSingleton<PlayerInventory>.Instance;
                if (inventoryInstance != null)
                    {
                    if (disableInteraction)
                        {
                        wasHotbarEnabled = inventoryInstance.HotbarEnabled;
                        wasEquippingEnabled = inventoryInstance.EquippingEnabled;
                        inventoryInstance.SetEquippingEnabled(false);
                        }
                    else
                        {
                        inventoryInstance.SetEquippingEnabled(wasEquippingEnabled);
                        }
                    }

                // Control movement
                PlayerMovement movementInstance = PlayerSingleton<PlayerMovement>.Instance;
                if (movementInstance != null)
                    {
                    movementInstance.canMove = !disableInteraction;
                    }
                }
            catch (Exception ex)
                {
                MelonLogger.Error($"Error controlling player interaction: {ex.Message}");
                }
            }
        }
    }
