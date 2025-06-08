using System.Reflection;
using Backpack.Config;
using MelonLoader;

[assembly: MelonInfo(typeof(Backpack.BackpackMod), "OG Backpack", "1.8.1", "D-Kay", "https://www.nexusmods.com/schedule1/mods/818")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: AssemblyMetadata("NexusModID", "818")]

namespace Backpack;

public class BackpackMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Configuration.Instance.Load();
        Configuration.Instance.Save(); // Save the default config to force the creation of the config file
        Logger.Info("Backpack initialized.");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Configuration.Instance.Reset();
        if (sceneName != "Main")
            return;

        ConfigSyncManager.StartSync();
    }
}