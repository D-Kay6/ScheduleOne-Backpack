using Harmony;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Product.Packaging;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
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
    private Backpack _equippedBackpack = null!;


    public PlayerBackpack(IntPtr ptr) : base(ptr)
    {
    }

    public static PlayerBackpack Instance { get; private set; }

    public bool IsUnlocked = true;

    //public bool IsUnlocked => NetworkSingleton<LevelManager>.Instance.GetFullRank() >= Configuration.Instance.UnlockLevel;
    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == StorageName;

    public Backpack GetCurrentBackpack() => _equippedBackpack;

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
    /// Sets the backpack storage to the specified number of slots.
    /// </summary>
    /// <remarks>The maximum number of slots for storage is 128.</remarks>
    /// <param name="slotCount">The number of slots.</param>
    public void SetSlots(int slotCount)
    {
        if (slotCount is < 1 or > MaxStorageSlots)
        {
            Logger.Warning("Cannot set backpack slots to {0} slots.", slotCount);
            return;
        }

        // Preserve existing items before resizing
        var previousItems = new List<ItemInstance>();
        foreach (var slot in _storage.ItemSlots)
        {
            var itemSlot = slot.TryCast<ItemSlot>();
            if (itemSlot == null)
            {
                continue;
            }

            if (itemSlot.ItemInstance != null)
            {
                previousItems.Add(itemSlot.ItemInstance);
            }
        }

        _storage.SlotCount = slotCount;
        _storage.DisplayRowCount = 1;
        _storage.ItemSlots.Clear();
        for (int i = 0; i < slotCount; i++)
        {
            _storage.ItemSlots.Add(new ItemSlot());
        }
        Logger.Info("Update backpack storage slots {0}.", slotCount);

        _storage.DisplayRowCount = slotCount switch
        {
            <= 20 => (int)Math.Ceiling(slotCount / 5.0),
            <= 80 => (int)Math.Ceiling(slotCount / 10.0),
            _ => (int)Math.Ceiling(slotCount / 16.0)
        };

        // Reinserting items into the new slots
        for (int i = 0; i < previousItems.Count && i < slotCount; i++)
        {
            _storage.ItemSlots[i].Cast<ItemSlot>().ItemInstance = previousItems[i];
        }
        _storage.ContentsChanged();
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
    public void OnEquipBackpack(Backpack backpack)
    {
        _equippedBackpack = backpack;
        _storage.StorageEntitySubtitle = backpack.Name;

        SetBackpackEnabled(true);
        SetSlots(backpack.Slots);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null!;
        }
    }
}