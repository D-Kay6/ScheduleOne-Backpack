using Il2CppScheduleOne.Levelling;
using MelonLoader;
using UnityEngine;

namespace Backpack;

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
        _enableSearchEntry = _generalCategory.CreateEntry("EnableSearch", false, "Enable police search for backpack items", null, true, true);

        _backpackCategory = MelonPreferences.CreateCategory("Backpack");
        _backpackCategory.SetFilePath(_configFile, false);
        _unlockLevelEntry = _backpackCategory.CreateEntry("UnlockLevel", new FullRank(ERank.Hoodlum, 1), "Required level to unlock");
        _storageSlotsEntry = _backpackCategory.CreateEntry("StorageSlots", 12, "Number of total storage slots");
    }

    public KeyCode ToggleKey
    {
        get => _toggleKeyEntry.Value;
        set => _toggleKeyEntry.Value = value;
    }

    public bool EnableSearch
    {
        get => _enableSearchEntry.Value;
        set => _enableSearchEntry.Value = value;
    }

    public FullRank UnlockLevel => new(_unlockLevelEntry.Value.Rank, Math.Clamp(_unlockLevelEntry.Value.Tier, 1, 5));
    public int StorageSlots => Math.Clamp(_storageSlotsEntry.Value, 1, PlayerBackpack.MaxStorageSlots);

    public void Load()
    {
        _generalCategory.LoadFromFile();
        _backpackCategory.LoadFromFile();
    }

    public void Save()
    {
        _generalCategory.SaveToFile();
        _backpackCategory.SaveToFile();
    }
}