using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.PlayerScripts;
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

    private bool _backpackEnabled = true;
    private StorageEntity _storage;

    public PlayerBackpack(IntPtr ptr) : base(ptr)
    {
    }

    public static PlayerBackpack Instance { get; private set; }

    public bool IsUnlocked => NetworkSingleton<LevelManager>.Instance.GetFullRank() >= Configuration.Instance.UnlockLevel;
    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == StorageName;

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

    public void OnStartClient(bool isOwner)
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

    public void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null!;
        }
    }
}