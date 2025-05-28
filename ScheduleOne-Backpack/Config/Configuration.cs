using Il2CppScheduleOne.Levelling;
using MelonLoader;
using UnityEngine;

namespace Backpack.Config;

public class Configuration
{
    private static Configuration _instance;
    public static Configuration Instance => _instance ??= new Configuration();

    private readonly string _configFile = Path.Combine("UserData", "Backpack.cfg");

    private readonly MelonPreferences_Category _generalCategory;
    private readonly MelonPreferences_Entry<KeyCode> _toggleKeyEntry;
    private readonly MelonPreferences_Entry<bool> _enableSearchEntry;

    private readonly MelonPreferences_Category _backpackCategory;
    private readonly MelonPreferences_Entry<FullRank> _unlockLevelEntry;
    private readonly MelonPreferences_Entry<int> _storageSlotsEntry;

    public Configuration()
    {
        _generalCategory = MelonPreferences.CreateCategory("General");
        _generalCategory.SetFilePath(_configFile, false);
        _toggleKeyEntry = _generalCategory.CreateEntry("ToggleKey", KeyCode.B, "Key to toggle backpack");
        _enableSearchEntry = _generalCategory.CreateEntry("EnableSearch", false, "Enable police search for backpack items");

        _backpackCategory = MelonPreferences.CreateCategory("Backpack");
        _backpackCategory.SetFilePath(_configFile, false);
        _unlockLevelEntry = _backpackCategory.CreateEntry("UnlockLevel", new FullRank(ERank.Hoodlum, 1), "Required level to unlock");
        _storageSlotsEntry = _backpackCategory.CreateEntry("StorageSlots", 12, "Number of total storage slots");
    }

    public KeyCode ToggleKey { get; set; }
    public bool EnableSearch { get; set; }
    public FullRank UnlockLevel { get; internal set; }
    public int StorageSlots { get; internal set; }

    public void Load()
    {
        MelonPreferences.Load();
        Reset();
    }

    public void Reset()
    {
        ToggleKey = _toggleKeyEntry.Value;
        EnableSearch = _enableSearchEntry.Value;
        UnlockLevel = new FullRank(_unlockLevelEntry.Value.Rank, Math.Clamp(_unlockLevelEntry.Value.Tier, 1, 5));
        StorageSlots = Math.Clamp(_storageSlotsEntry.Value, 1, PlayerBackpack.MaxStorageSlots);
    }

    public void Save()
    {
        _toggleKeyEntry.Value = ToggleKey;
        _enableSearchEntry.Value = EnableSearch;
        _unlockLevelEntry.Value = new FullRank(UnlockLevel.Rank, Math.Clamp(UnlockLevel.Tier, 1, 5));
        _storageSlotsEntry.Value = Math.Clamp(StorageSlots, 1, PlayerBackpack.MaxStorageSlots);
        MelonPreferences.Save();
    }
}