using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Storage;
using ScheduleOne.Tools;
using ScheduleOne.UI;
using UnityEngine;

namespace BackpackMod;

public class PlayerBackpack : MonoBehaviour
{
    public const string StorageName = "Backpack";
    private const KeyCode ToggleKey = KeyCode.B;

    private bool _backpackEnabled = true;
    private StorageEntity _storage;

    public PlayerBackpack(IntPtr ptr) : base(ptr)
    {

    }

    public static PlayerBackpack Instance { get; private set; }

    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == StorageName;

    private void Awake()
    {
        _storage = gameObject.GetComponentInParent<StorageEntity>();
        if (_storage == null)
        {
            BackpackMod.Log.LogError("Player does not have a BackpackStorage component!");
            return;
        }

        if (_storage.SlotCount != 12)
        {
            BackpackMod.Log.LogWarning("Backpack storage not initialized. Reinitializing.");
            _storage.SlotCount = 12;
            _storage.DisplayRowCount = 3;
            _storage.StorageEntityName = StorageName;
            _storage.StorageEntitySubtitle = string.Empty;
            _storage.MaxAccessDistance = float.PositiveInfinity;
            for (var i = _storage.ItemSlots.Count; i < _storage.SlotCount; i++)
            {
                var itemSlot = new ItemSlot();
                itemSlot.onItemDataChanged.CombineImpl((Il2CppSystem.Action) _storage.ContentsChanged);
                _storage.ItemSlots.Add(itemSlot);
            }
        }

        OnStartClient(true);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(ToggleKey) || !_backpackEnabled)
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
            BackpackMod.Log.LogError("Error toggling backpack: " + e.Message);
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
        if (!_backpackEnabled || Singleton<ManagementClipboard>.Instance.IsEquipped || Singleton<StorageMenu>.Instance.IsOpen)
            return;

        var storageMenu = Singleton<StorageMenu>.Instance;
        storageMenu.SlotGridLayout.constraintCount = 3;
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

    public void OnStartClient(bool isOwner)
    {
        if (!isOwner)
        {
            BackpackMod.Log.LogInfo("Destroying non-local player singleton: " + name);
            Destroy(this);
            return;
        }

        if (Instance != null)
        {
            BackpackMod.Log.LogWarning("Multiple instances of " + name + " exist. Keeping prior instance reference.");
            return;
        }

        Instance = this;
    }

    public void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null!;
        }
    }
}