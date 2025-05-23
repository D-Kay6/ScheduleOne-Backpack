﻿using HarmonyLib;
using Il2CppFishNet.Component.Spawning;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Storage;
using Il2CppVLB;

namespace Backpack.Patches;

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
            Logger.Error("Player prefab is null!");
            return;
        }

        var player = playerPrefab.GetComponent<Player>();
        if (player == null)
        {
            Logger.Error("Player prefab does not have a Player component!");
            return;
        }

        var storage = player.gameObject.GetOrAddComponent<StorageEntity>();
        storage.SlotCount = Configuration.Instance.StorageSlots;
        storage.DisplayRowCount = storage.SlotCount switch
        {
            <= 20 => (int) Math.Ceiling(storage.SlotCount / 5.0),
            <= 80 => (int) Math.Ceiling(storage.SlotCount / 10.0),
            _ => (int) Math.Ceiling(storage.SlotCount / 16.0)
        };
        storage.StorageEntityName = PlayerBackpack.StorageName;
        storage.MaxAccessDistance = float.PositiveInfinity;

        player.LocalGameObject.GetOrAddComponent<PlayerBackpack>();
    }
}