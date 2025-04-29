using HarmonyLib;
using FishNet.Component.Spawning;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Storage;
using VLB;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(PlayerSpawner))]
public static class PlayerSpawnerPatch
{
    [HarmonyPatch("InitializeOnce")]
    [HarmonyPostfix]
    public static void InitializeOnce(PlayerSpawner __instance)
    {
        var playerPrefab = __instance._playerPrefab;
        if (!playerPrefab)
        {
            BackpackMod.Log.LogError("Player prefab is null!");
            return;
        }

        var player = playerPrefab.GetComponent<Player>();
        if (player == null)
        {
            BackpackMod.Log.LogError("Player prefab does not have a Player component!");
            return;
        }

        var storage = player.gameObject.GetOrAddComponent<StorageEntity>();
        storage.SlotCount = 12;
        storage.DisplayRowCount = 3;
        storage.StorageEntityName = PlayerBackpack.StorageName;
        storage.MaxAccessDistance = float.PositiveInfinity;
        player.LocalGameObject.GetOrAddComponent<PlayerBackpack>();
    }
}