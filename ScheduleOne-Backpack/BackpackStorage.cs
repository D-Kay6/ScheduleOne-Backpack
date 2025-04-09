using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI;
using Il2CppFishNet.Connection;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.Persistence.Datas;
using MelonLoader;
using UnityEngine;

namespace BackpackMod;

public class BackpackStorage : MonoBehaviour
{
    private const int Columns = 4;
    private const int Rows = 3;

    public int SlotCount = Rows * Columns;
    public int DisplayRowCount = Rows;
    public string StorageEntityName = "Backpack Storage";
    public string StorageEntitySubtitle = string.Empty;

    public BackpackStorage(IntPtr ptr) : base(ptr)
    {
    }

    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == StorageEntityName;

    public Il2CppSystem.Collections.Generic.List<ItemSlot> ItemSlots { get; set; } = new();

    public void SetStoredInstance(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (instance == null)
        {
            ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().ClearStoredInstance(true);
            return;
        }

        ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().SetStoredItem(instance, true);
    }

    public void SetItemSlotQuantity(int itemSlotIndex, int quantity)
    {
        ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().SetQuantity(quantity, true);
    }

    public void SetSlotLocked(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (locked)
        {
            ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().ApplyLock(lockOwner, lockReason, true);
            return;
        }

        ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().RemoveLock(true);
    }

    public void Awake()
    {
        for (var i = 0; i < SlotCount; i++)
        {
            var itemSlot = new ItemSlot();
            itemSlot.SetSlotOwner(new IItemSlotOwner(Pointer));
        }
    }

    public void Start()
    {
        var instance = NetworkSingleton<MoneyManager>.Instance;
        instance.onNetworthCalculation.CombineImpl((Il2CppSystem.Action<MoneyManager.FloatContainer>) GetNetworth);
    }

    public void OnDestroy()
    {
        if (!NetworkSingleton<MoneyManager>.InstanceExists)
            return;

        var instance = NetworkSingleton<MoneyManager>.Instance;
        instance.onNetworthCalculation.CombineImpl((Il2CppSystem.Action<MoneyManager.FloatContainer>) GetNetworth);
    }

    public bool CanBeOpened()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main" && !Singleton<StorageMenu>.Instance.IsOpen;
    }

    public void Open()
    {
        if (!CanBeOpened())
        {
            Melon<BackpackMod>.Logger.Warning("StorageEntity Open() called but CanBeOpened() returned false");
            return;
        }

        var storageMenu = Singleton<StorageMenu>.Instance;
        storageMenu.SlotGridLayout.constraintCount = DisplayRowCount;
        storageMenu.Open(StorageEntityName, StorageEntitySubtitle, new IItemSlotOwner(Pointer));
    }

    public void Close()
    {
        if (!IsOpen)
        {
            Melon<BackpackMod>.Logger.Warning("IItemSlotOwner Close() called but IsOpen == false");
            return;
        }

        Singleton<StorageMenu>.Instance.CloseMenu();
    }

    private void GetNetworth(MoneyManager.FloatContainer container)
    {
        for (var i = 0; i < ItemSlots.Count; i++)
        {
            if (ItemSlots[new Index(i)].Cast<ItemSlot>()!.ItemInstance != null)
            {
                container.ChangeValue(ItemSlots[new Index(i)].Cast<ItemSlot>().ItemInstance.GetMonetaryValue());
            }
        }
    }
}