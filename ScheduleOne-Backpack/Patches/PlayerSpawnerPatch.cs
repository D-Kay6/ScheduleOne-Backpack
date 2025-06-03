using HarmonyLib;

#if IL2CPP
using Il2CppFishNet.Component.Spawning;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Storage;
using Il2CppVLB;
#elif MONO
using FishNet.Component.Spawning;
using FishNet.Object;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Storage;
using VLB;
#endif

namespace Backpack.Patches;

[HarmonyPatch(typeof(PlayerSpawner))]
public static class PlayerSpawnerPatch
{
    [HarmonyPatch("InitializeOnce")]
    [HarmonyPostfix]
    public static void InitializeOnce(PlayerSpawner __instance)
#if IL2CPP
    {
        var playerPrefab = __instance._playerPrefab;
#elif MONO
    {
        var traverser = new Traverse(__instance);
        var playerPrefab = traverser.Field("_playerPrefab").GetValue<NetworkObject>();;
#endif
        if (!playerPrefab)
        {
            Logger.Error("Player prefab is null!");
            return;
        }

        var player = playerPrefab.GetComponent<Player>();
        if (player == null)
        {
            Logger.Error("Player prefab does not have a Player component!");
            return;
        }

        Logger.Info("Adding backpack storage to player prefab...");
        var storage = player.gameObject.GetOrAddComponent<StorageEntity>();
        storage.StorageEntityName = PlayerBackpack.StorageName;
        storage.MaxAccessDistance = float.PositiveInfinity;
        player.LocalGameObject.GetOrAddComponent<PlayerBackpack>();
    }
}
