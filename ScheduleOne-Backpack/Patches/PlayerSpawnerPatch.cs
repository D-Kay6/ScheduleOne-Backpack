using HarmonyLib;
using Il2CppFishNet.Component.Spawning;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppVLB;
using MelonLoader;

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
            Melon<BackpackMod>.Logger.Error("Player prefab is null!");
            return;
        }

        var player = playerPrefab.GetComponent<Player>();
        if (player == null)
        {
            Melon<BackpackMod>.Logger.Error("Player prefab does not have a Player component!");
            return;
        }

        player.gameObject.GetOrAddComponent<BackpackStorage>();
        player.LocalGameObject.GetOrAddComponent<PlayerBackpack>();
    }
}