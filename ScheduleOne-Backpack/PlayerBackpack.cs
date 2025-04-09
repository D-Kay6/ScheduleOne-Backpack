using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using UnityEngine;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class PlayerBackpack : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.B;

    private bool _backpackEnabled = true;
    private BackpackStorage _backpackStorage;

    public PlayerBackpack(IntPtr ptr) : base(ptr)
    {
    }

    public static bool InstanceExists => Instance != null;

    public static PlayerBackpack Instance { get; protected set; }

    public bool IsOpen => _backpackStorage.IsOpen;

    public void Awake()
    {
        _backpackStorage = gameObject.AddComponent<BackpackStorage>();
        OnStartClient(true);
    }

    public void Update()
    {
        if (!Input.GetKeyDown(ToggleKey))
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
            _backpackStorage.Close();
    }

    public void Open()
    {
        if (!_backpackEnabled || IsOpen)
        {
            Melon<BackpackMod>.Logger.Warning("Backpack is already open or backpack is disabled.");
            return;
        }

        if (_backpackStorage == null)
        {
            Melon<BackpackMod>.Logger.Error("BackpackStorage is null!");
            return;
        }

        _backpackStorage.Open();
    }

    public void Close()
    {
        if (!_backpackEnabled || !IsOpen)
            return;

        _backpackStorage.Close();
    }

    public string GetBackpackString()
    {
        if (_backpackStorage == null)
        {
            Melon<BackpackMod>.Logger.Error("BackpackStorage is null!");
            return string.Empty;
        }

        return new ItemSet(_backpackStorage.ItemSlots).GetJSON();
    }

    public void LoadBackpack(string contentsString)
    {
        if (string.IsNullOrEmpty(contentsString))
        {
            Melon<BackpackMod>.Logger.Warning("Empty backpack string");
            return;
        }

        if (!Player.Local.IsOwner || _backpackStorage == null)
            return;

        var items = ItemSet.Deserialize(contentsString);
        if (items == null)
        {
            Melon<BackpackMod>.Logger.Error("Failed to deserialize backpack string");
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (i >= _backpackStorage.SlotCount)
            {
                Melon<BackpackMod>.Logger.Error($"Item slot index {i} out of range");
                break;
            }

            _backpackStorage.ItemSlots[new Index(i)].Cast<ItemSlot>().SetStoredItem(item);
        }
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