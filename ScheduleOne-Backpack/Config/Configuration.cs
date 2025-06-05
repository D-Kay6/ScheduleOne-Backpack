using MelonLoader;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.Levelling;
#elif MONO
using ScheduleOne.Levelling;
#endif

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

    public Configuration()
    {
        _generalCategory = MelonPreferences.CreateCategory("General");
        _generalCategory.SetFilePath(_configFile, false);
        _toggleKeyEntry = _generalCategory.CreateEntry("ToggleKey", KeyCode.B, "Key to toggle backpack");
        _enableSearchEntry = _generalCategory.CreateEntry("EnableSearch", false, "Enable police search for backpack items");

        _backpackCategory = MelonPreferences.CreateCategory("Backpack");
        _backpackCategory.SetFilePath(_configFile, false);
    }

    public KeyCode ToggleKey { get; set; }
    public bool EnableSearch { get; set; }

    public void Load()
    {
        MelonPreferences.Load();
        Reset();
    }

    public void Reset()
    {
        ToggleKey = _toggleKeyEntry.Value;
        EnableSearch = _enableSearchEntry.Value;
    }

    public void Save()
    {
        _toggleKeyEntry.Value = ToggleKey;
        _enableSearchEntry.Value = EnableSearch;
        MelonPreferences.Save();
    }
}
