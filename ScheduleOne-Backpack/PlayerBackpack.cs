using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using MelonLoader;
using UnityEngine;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
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
            Melon<BackpackMod>.Logger.Error("Player does not have a BackpackStorage component!");
            return;
        }

        if (_storage.SlotCount != 12)
        {
            Melon<BackpackMod>.Logger.Warning("Backpack storage not initialized. Reinitializing.");
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
            Melon<BackpackMod>.Logger.Error("Error toggling backpack: " + e.Message);
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
        if (!_backpackEnabled || Singleton<ManagementClipboard>.Instance.IsEquipped || Singleton<StorageMenu>.Instance.IsOpen || Phone.instance.IsOpen)
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
            Melon<BackpackMod>.Logger.Msg("Destroying non-local player singleton: " + name, null);
            Destroy(this);
            return;
        }

        if (Instance != null)
        {
            Melon<BackpackMod>.Logger.Warning("Multiple instances of " + name + " exist. Keeping prior instance reference.", null);
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