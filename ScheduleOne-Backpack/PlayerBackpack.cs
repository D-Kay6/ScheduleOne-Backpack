using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Product.Packaging;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppSystem.Linq;
using MelonLoader;
using UnityEngine;

namespace Backpack;

[RegisterTypeInIl2Cpp]
public class PlayerBackpack : MonoBehaviour
{
    public const string StorageName = "Backpack";
    public const int MaxStorageSlots = 128;

    private bool _backpackEnabled = true;
    private StorageEntity _storage;

    public PlayerBackpack(IntPtr ptr) : base(ptr)
    {
    }

    public static PlayerBackpack Instance { get; private set; }

    public bool IsUnlocked => NetworkSingleton<LevelManager>.Instance.GetFullRank() >= Configuration.Instance.UnlockLevel;
    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == StorageName;
    public Il2CppSystem.Collections.Generic.List<ItemSlot> ItemSlots => _storage.ItemSlots.Cast<Il2CppSystem.Collections.Generic.IEnumerable<ItemSlot>>().ToList();

    private void Awake()
    {
        _storage = gameObject.GetComponentInParent<StorageEntity>();
        if (_storage == null)
        {
            Logger.Error("Player does not have a BackpackStorage component!");
            return;
        }

        OnStartClient(true);
    }

    private void Update()
    {
        if (!_backpackEnabled || !IsUnlocked || !Input.GetKeyDown(Configuration.Instance.ToggleKey))
            return;

        try
        {
            if (IsOpen)
                Close();
            else
                Open();
        }
        catch (Exception e)
        {
            Logger.Error("Error toggling backpack: " + e.Message);
        }
    }

    public void SetBackpackEnabled(bool enabled)
    {
        if (!enabled)
            Close();

        _backpackEnabled = enabled;
    }

    public void Open()
    {
        if (!_backpackEnabled || !IsUnlocked || Singleton<ManagementClipboard>.Instance.IsEquipped || Singleton<StorageMenu>.Instance.IsOpen || Phone.instance.IsOpen)
            return;

        var storageMenu = Singleton<StorageMenu>.Instance;
        storageMenu.SlotGridLayout.constraintCount = _storage.DisplayRowCount;
        storageMenu.Open(StorageName, string.Empty, _storage.Cast<IItemSlotOwner>());
        _storage.SendAccessor(Player.Local.NetworkObject);
    }

    public void Close()
    {
        if (!_backpackEnabled || !IsOpen)
            return;

        Singleton<StorageMenu>.Instance.CloseMenu();
        _storage.SendAccessor(null);
    }

    /// <summary>
    /// Adds the specified number of slots to the backpack.
    /// </summary>
    /// <remarks>The maximum number of slots for storage is 128.</remarks>
    /// <param name="slotCount">The number of slots to add.</param>
    public void Upgrade(int slotCount)
    {
        if (slotCount is < 1 or > MaxStorageSlots)
            return;

        var newSlotCount = _storage.SlotCount + slotCount;
        if (newSlotCount > MaxStorageSlots)
        {
            Logger.Warning("Cannot upgrade backpack to more than {0} slots.", MaxStorageSlots);
            return;
        }

        _storage.SlotCount = newSlotCount;
        _storage.DisplayRowCount = newSlotCount switch
        {
            <= 20 => (int) Math.Ceiling(newSlotCount / 5.0),
            <= 80 => (int) Math.Ceiling(newSlotCount / 10.0),
            _ => (int) Math.Ceiling(newSlotCount / 16.0)
        };

        for (var i = _storage.ItemSlots.Count; i < newSlotCount; i++)
        {
            var itemSlot = new ItemSlot();
            itemSlot.onItemDataChanged.CombineImpl((Il2CppSystem.Action) _storage.ContentsChanged);
            itemSlot.SetSlotOwner(_storage.Cast<IItemSlotOwner>());
        }
    }

    public bool ContainsItemsOfInterest(EStealthLevel maxStealthLevel)
    {
        for (var i = 0; i < _storage.ItemSlots.Count; i++)
        {
            var itemSlot = _storage.ItemSlots[new Index(i)].Cast<ItemSlot>();
            if (itemSlot?.ItemInstance == null)
                continue;

            var productInstance = itemSlot.ItemInstance.TryCast<ProductItemInstance>();
            if (productInstance == null)
            {
                if (itemSlot.ItemInstance.Definition.legalStatus != ELegalStatus.Legal)
                    return true;

                continue;
            }

            if (productInstance.AppliedPackaging == null || productInstance.AppliedPackaging.StealthLevel <= maxStealthLevel)
                return true;
        }

        return false;
    }

    private void OnStartClient(bool isOwner)
    {
        if (!isOwner)
        {
            Logger.Info("Destroying non-local player singleton: " + name, null);
            Destroy(this);
            return;
        }

        if (Instance != null)
        {
            Logger.Warning("Multiple instances of " + name + " exist. Keeping prior instance reference.", null);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null!;
        }
    }
}