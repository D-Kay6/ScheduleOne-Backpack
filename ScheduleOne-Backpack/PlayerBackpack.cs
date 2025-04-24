using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class PlayerBackpack : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.B;

    private bool _backpackEnabled = true;
    private BackpackStorage _storage;

    public PlayerBackpack(IntPtr ptr) : base(ptr)
    {
    }

    public static PlayerBackpack Instance { get; private set; }

    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == _storage.StorageEntityName;

    private void Awake()
    {
        _storage = gameObject.GetComponentInParent<BackpackStorage>();
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
        _backpackEnabled = enabled;
        if (!enabled && IsOpen)
            _storage.Close();
    }

    public void Open()
    {
        if (!_backpackEnabled || IsOpen)
        {
            Melon<BackpackMod>.Logger.Warning("Backpack is already open or backpack is disabled.");
            return;
        }

        _storage.Open();
    }

    public void Close()
    {
        if (!_backpackEnabled || !IsOpen)
            return;

        _storage.Close();
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