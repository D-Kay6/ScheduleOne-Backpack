using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(Backpack.BackpackMod), "OG Backpack", "1.7.0", "D-Kay", "https://www.nexusmods.com/schedule1/mods/818")]
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
}