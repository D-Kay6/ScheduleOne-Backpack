using Il2CppFishNet;
using Il2CppFishNet.Connection;
using Il2CppFishNet.Object;
using Il2CppFishNet.Object.Delegating;
using Il2CppFishNet.Object.Prediction.Delegating;
using Il2CppFishNet.Object.Synchronizing.Internal;
using Il2CppFishNet.Serializing;
using Il2CppFishNet.Transporting;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI;
using MelonLoader;

namespace BackpackMod;

[RegisterTypeInIl2CppWithInterfaces(typeof(IItemSlotOwner))]
public class BackpackStorage : NetworkBehaviour
{
    private const int Columns = 4;
    private const int Rows = 3;

    private bool _initializeEarlyExecuted;
    private bool _initializeLateExecuted;

    public int SlotCount = Rows * Columns;
    public int DisplayRowCount = Rows;
    public string StorageEntityName = "Backpack Storage";
    public string StorageEntitySubtitle = string.Empty;

    public BackpackStorage(IntPtr ptr) : base(ptr)
    {
        // These are all the default values for the fields that are set in the base class
        // Because NetworkBehaviour is abstract, we need to set them ourselves
        _componentIndexCache = byte.MaxValue;
        _replicateRpcDelegates = new Il2CppSystem.Collections.Generic.Dictionary<uint, ReplicateRpcDelegate>();
        _reconcileRpcDelegates = new Il2CppSystem.Collections.Generic.Dictionary<uint, ReconcileRpcDelegate>();
        _rpcLinks = new Il2CppSystem.Collections.Generic.Dictionary<uint, RpcLinkType>();
        _serverRpcDelegates = new Il2CppSystem.Collections.Generic.Dictionary<uint, ServerRpcDelegate>();
        _observersRpcDelegates = new Il2CppSystem.Collections.Generic.Dictionary<uint, ClientRpcDelegate>();
        _targetRpcDelegates = new Il2CppSystem.Collections.Generic.Dictionary<uint, ClientRpcDelegate>();
        _rpcHashSize = 1;
        _bufferedRpcs = new Il2CppSystem.Collections.Generic.Dictionary<uint, BufferedRpc>();
        _networkConnectionCache = new Il2CppSystem.Collections.Generic.HashSet<NetworkConnection>();
        _syncVars = new Il2CppSystem.Collections.Generic.Dictionary<uint, SyncBase>();
        _syncObjects = new Il2CppSystem.Collections.Generic.Dictionary<uint, SyncBase>();
        _syncVarReadDelegates = new Il2CppSystem.Collections.Generic.List<SyncVarReadDelegate>();
        // If you're going to use this code, have the decency to give credit to the original author
    }

    public bool IsOpen => Singleton<StorageMenu>.Instance.IsOpen && Singleton<StorageMenu>.Instance.TitleLabel.text == StorageEntityName;

    public Il2CppSystem.Collections.Generic.List<ItemSlot> ItemSlots { get; set; } = new();

    public void SetStoredInstance(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        RpcWriter___Server_SetStoredInstance(conn, itemSlotIndex, instance);
        RpcLogic___SetStoredInstance(conn, itemSlotIndex, instance);
    }

    private void SetStoredInstance_Internal(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (conn == null)
        {
            RpcWriter___Observers_SetStoredInstance_Internal(conn, itemSlotIndex, instance);
            RpcLogic___SetStoredInstance_Internal(conn, itemSlotIndex, instance);
        }
        else
        {
            RpcWriter___Target_SetStoredInstance_Internal(conn, itemSlotIndex, instance);
            RpcLogic___SetStoredInstance_Internal(conn, itemSlotIndex, instance);
        }
    }

    public void SetItemSlotQuantity(int itemSlotIndex, int quantity)
    {
        RpcWriter___Server_SetItemSlotQuantity(itemSlotIndex, quantity);
        RpcLogic___SetItemSlotQuantity(itemSlotIndex, quantity);
    }

    private void SetItemSlotQuantity_Internal(int itemSlotIndex, int quantity)
    {
        RpcWriter___Observers_SetItemSlotQuantity_Internal(itemSlotIndex, quantity);
        RpcLogic___SetItemSlotQuantity_Internal(itemSlotIndex, quantity);
    }

    public void SetSlotLocked(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        RpcWriter___Server_SetSlotLocked(conn, itemSlotIndex, locked, lockOwner, lockReason);
        RpcLogic___SetSlotLocked(conn, itemSlotIndex, locked, lockOwner, lockReason);
    }

    private void SetSlotLocked_Internal(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (conn == null)
        {
            RpcWriter___Observers_SetSlotLocked_Internal(conn, itemSlotIndex, locked, lockOwner, lockReason);
        }
        else
        {
            RpcWriter___Target_SetSlotLocked_Internal(conn, itemSlotIndex, locked, lockOwner, lockReason);
        }

        RpcLogic___SetSlotLocked_Internal(conn, itemSlotIndex, locked, lockOwner, lockReason);
    }

    public void Awake()
    {
        NetworkInitialize___Early();
        for (var i = 0; i < SlotCount; i++)
        {
            var itemSlot = new ItemSlot();
            itemSlot.SetSlotOwner(new IItemSlotOwner(Pointer));
        }

        NetworkInitialize__Late();
    }

    public bool CanBeOpened()
    {
        return !Singleton<ManagementClipboard>.Instance.IsEquipped && !Singleton<StorageMenu>.Instance.IsOpen;
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

    public string GetContentString()
    {
        return new ItemSet(ItemSlots).GetJSON();
    }

    public void LoadContents(string contentsString)
    {
        if (string.IsNullOrEmpty(contentsString))
        {
            Melon<BackpackMod>.Logger.Warning("Empty backpack string");
            return;
        }

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

            if (i >= SlotCount)
            {
                Melon<BackpackMod>.Logger.Error($"Item slot index {i} out of range");
                break;
            }

            ItemSlots[new Index(i)].Cast<ItemSlot>().SetStoredItem(item);
        }
    }

    public void GetNetworth(MoneyManager.FloatContainer container)
    {
        for (var i = 0; i < ItemSlots.Count; i++)
        {
            if (ItemSlots[new Index(i)].Cast<ItemSlot>().ItemInstance != null)
            {
                container.ChangeValue(ItemSlots[new Index(i)].Cast<ItemSlot>().ItemInstance.GetMonetaryValue());
            }
        }
    }

    #region NETWORKING
    // We could probably use the StorageEntity class for this, but making this ourselves allows us to better limit the use of the backpack

    internal void NetworkInitializeIfDisabled_Internal()
    {
        NetworkInitialize___Early();
        NetworkInitialize__Late();
    }

    private void NetworkInitialize___Early()
    {
        if (_initializeEarlyExecuted)
            return;

        _initializeEarlyExecuted = true;
        try
        {
            RegisterServerRpc(0U, (ServerRpcDelegate) RpcReader___Server_SetStoredInstance);
            RegisterObserversRpc(1U, (ClientRpcDelegate) RpcReader___Observers_SetStoredInstance_Internal);
            RegisterTargetRpc(2U, (ClientRpcDelegate) RpcReader___Target_SetStoredInstance_Internal);
            RegisterServerRpc(3U, (ServerRpcDelegate) RpcReader___Server_SetItemSlotQuantity);
            RegisterObserversRpc(4U, (ClientRpcDelegate) RpcReader___Observers_SetItemSlotQuantity_Internal);
            RegisterServerRpc(5U, (ServerRpcDelegate) RpcReader___Server_SetSlotLocked);
            RegisterTargetRpc(6U, (ClientRpcDelegate) RpcReader___Target_SetSlotLocked_Internal);
            RegisterObserversRpc(7U, (ClientRpcDelegate) RpcReader___Observers_SetSlotLocked_Internal);
        }
        catch (Exception e)
        {
            Melon<BackpackMod>.Logger.Error($"Error during NetworkInitialize___Early: {e.Message}", e);
            throw;
        }
    }

    private void NetworkInitialize__Late()
    {
        if (_initializeLateExecuted)
            return;

        _initializeLateExecuted = true;
    }

    private void RpcWriter___Server_SetStoredInstance(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (!IsClientInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteNetworkConnection(conn);
            writer.WriteInt32(itemSlotIndex);
            writer.WriteItemInstance(instance);
            SendServerRpc(0U, writer, channel, DataOrderType.Default);
            writer.Store();
        }
    }

    private void RpcLogic___SetStoredInstance(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (conn == null || conn.ClientId == -1)
            SetStoredInstance_Internal(null, itemSlotIndex, instance);
        else
            SetStoredInstance_Internal(conn, itemSlotIndex, instance);
    }

    private void RpcReader___Server_SetStoredInstance(PooledReader pooledReader, Channel channel, NetworkConnection conn)
    {
        var conn1 = pooledReader.ReadNetworkConnection();
        var itemSlotIndex = pooledReader.ReadInt32();
        var instance = pooledReader.ReadItemInstance();
        if (!IsServerInitialized || conn.IsLocalClient)
            return;

        RpcLogic___SetStoredInstance(conn1, itemSlotIndex, instance);
    }

    private void RpcWriter___Observers_SetStoredInstance_Internal(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (!IsServerInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteInt32(itemSlotIndex);
            writer.WriteItemInstance(instance);
            SendObserversRpc(1U, writer, channel, DataOrderType.Default, false, false, false);
            writer.Store();
        }
    }

    private void RpcLogic___SetStoredInstance_Internal(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (instance == null)
        {
            ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().ClearStoredInstance(true);
            return;
        }

        ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().SetStoredItem(instance, true);
    }

    private void RpcReader___Observers_SetStoredInstance_Internal(PooledReader pooledReader, Channel channel)
    {
        var itemSlotIndex = pooledReader.ReadInt32();
        var instance = pooledReader.ReadItemInstance();
        if (!IsClientInitialized || IsHost)
            return;

        RpcLogic___SetStoredInstance_Internal(null, itemSlotIndex, instance);
    }

    private void RpcWriter___Target_SetStoredInstance_Internal(NetworkConnection conn, int itemSlotIndex, ItemInstance instance)
    {
        if (!IsServerInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteInt32(itemSlotIndex);
            writer.WriteItemInstance(instance);
            SendTargetRpc(2U, writer, channel, DataOrderType.Default, conn, false);
            writer.Store();
        }
    }

    private void RpcReader___Target_SetStoredInstance_Internal(PooledReader pooledReader, Channel channel)
    {
        var itemSlotIndex = pooledReader.ReadInt32();
        var instance = pooledReader.ReadItemInstance();
        if (!IsClientInitialized || IsHost)
            return;

        RpcLogic___SetStoredInstance_Internal(LocalConnection, itemSlotIndex, instance);
    }


    private void RpcWriter___Server_SetItemSlotQuantity(int itemSlotIndex, int quantity)
    {
        if (!IsClientInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteInt32(itemSlotIndex);
            writer.WriteInt32(quantity);
            SendServerRpc(3U, writer, channel, DataOrderType.Default);
            writer.Store();
        }
    }

    private void RpcLogic___SetItemSlotQuantity(int itemSlotIndex, int quantity)
    {
        SetItemSlotQuantity_Internal(itemSlotIndex, quantity);
    }

    private void RpcReader___Server_SetItemSlotQuantity(PooledReader pooledReader, Channel channel, NetworkConnection conn)
    {
        var itemSlotIndex = pooledReader.ReadInt32();
        var quantity = pooledReader.ReadInt32();
        if (!IsServerInitialized || conn.IsLocalClient)
            return;

        RpcLogic___SetItemSlotQuantity(itemSlotIndex, quantity);
    }

    private void RpcWriter___Observers_SetItemSlotQuantity_Internal(int itemSlotIndex, int quantity)
    {
        if (!IsServerInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteInt32(itemSlotIndex);
            writer.WriteInt32(quantity);
            SendObserversRpc(4U, writer, channel, DataOrderType.Default, false, false, false);
            writer.Store();
        }
    }

    private void RpcLogic___SetItemSlotQuantity_Internal(int itemSlotIndex, int quantity)
    {
        ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().SetQuantity(quantity, true);
    }

    private void RpcReader___Observers_SetItemSlotQuantity_Internal(PooledReader pooledReader, Channel channel)
    {
        var itemSlotIndex = pooledReader.ReadInt32();
        var quantity = pooledReader.ReadInt32();
        if (!IsClientInitialized || IsHost)
            return;

        RpcLogic___SetItemSlotQuantity_Internal(itemSlotIndex, quantity);
    }


    private void RpcWriter___Server_SetSlotLocked(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (!IsClientInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteNetworkConnection(conn);
            writer.WriteInt32(itemSlotIndex);
            writer.WriteBoolean(locked);
            writer.WriteNetworkObject(lockOwner);
            writer.WriteString(lockReason);
            SendServerRpc(5U, writer, channel, DataOrderType.Default);
            writer.Store();
        }
    }

    private void RpcLogic___SetSlotLocked(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (conn == null || conn.ClientId == -1)
            SetSlotLocked_Internal(null, itemSlotIndex, locked, lockOwner, lockReason);
        else
            SetSlotLocked_Internal(conn, itemSlotIndex, locked, lockOwner, lockReason);
    }

    private void RpcReader___Server_SetSlotLocked(PooledReader pooledReader, Channel channel, NetworkConnection conn)
    {
        var conn1 = pooledReader.ReadNetworkConnection();
        var itemSlotIndex = pooledReader.ReadInt32();
        var locked = pooledReader.ReadBoolean();
        var lockOwner = pooledReader.ReadNetworkObject();
        var lockReason = pooledReader.ReadString();
        if (!IsServerInitialized || conn.IsLocalClient)
            return;

        RpcLogic___SetSlotLocked(conn1, itemSlotIndex, locked, lockOwner, lockReason);
    }

    private void RpcWriter___Target_SetSlotLocked_Internal(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (!IsServerInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteInt32(itemSlotIndex);
            writer.WriteBoolean(locked);
            writer.WriteNetworkObject(lockOwner);
            writer.WriteString(lockReason);
            SendTargetRpc(6U, writer, channel, DataOrderType.Default, conn, false);
            writer.Store();
        }
    }

    private void RpcLogic___SetSlotLocked_Internal(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (locked)
        {
            ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().ApplyLock(lockOwner, lockReason, true);
            return;
        }

        ItemSlots[new Index(itemSlotIndex)].Cast<ItemSlot>().RemoveLock(true);
    }

    private void RpcReader___Target_SetSlotLocked_Internal(PooledReader pooledReader, Channel channel)
    {
        var itemSlotIndex = pooledReader.ReadInt32();
        var locked = pooledReader.ReadBoolean();
        var lockOwner = pooledReader.ReadNetworkObject();
        var lockReason = pooledReader.ReadString();
        if (!IsClientInitialized || IsHost)
            return;

        RpcLogic___SetSlotLocked_Internal(LocalConnection, itemSlotIndex, locked, lockOwner, lockReason);
    }

    private void RpcWriter___Observers_SetSlotLocked_Internal(NetworkConnection conn, int itemSlotIndex, bool locked, NetworkObject lockOwner, string lockReason)
    {
        if (!IsServerInitialized)
        {
            var networkManager = NetworkManager ?? InstanceFinder.NetworkManager;
            if (networkManager != null)
                networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
            else
                Melon<BackpackMod>.Logger.Warning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
        }
        else
        {
            var channel = Channel.Reliable;
            var writer = WriterPool.GetWriter();
            writer.WriteInt32(itemSlotIndex);
            writer.WriteBoolean(locked);
            writer.WriteNetworkObject(lockOwner);
            writer.WriteString(lockReason);
            SendObserversRpc(7U, writer, channel, DataOrderType.Default, false, false, false);
            writer.Store();
        }
    }

    private void RpcReader___Observers_SetSlotLocked_Internal(PooledReader pooledReader, Channel channel)
    {
        var itemSlotIndex = pooledReader.ReadInt32();
        var locked = pooledReader.ReadBoolean();
        var lockOwner = pooledReader.ReadNetworkObject();
        var lockReason = pooledReader.ReadString();
        if (!IsClientInitialized || IsHost)
            return;

        RpcLogic___SetSlotLocked_Internal(null, itemSlotIndex, locked, lockOwner, lockReason);
    }

    #endregion
}