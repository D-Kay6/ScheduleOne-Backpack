using Il2CppScheduleOne.PlayerScripts;

namespace BackpackMod;

public static class PlayerExtensions
{
    public static BackpackStorage GetBackpackStorage(this Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        var backpackStorage = player.gameObject.GetComponent<BackpackStorage>();
        if (backpackStorage == null)
            throw new InvalidOperationException("Player does not have a BackpackStorage component.");

        return backpackStorage;
    }
}