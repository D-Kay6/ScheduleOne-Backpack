using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(Backpack.BackpackMod), "OG Backpack", "1.6.0", "D-Kay", "https://www.nexusmods.com/schedule1/mods/818")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: AssemblyMetadata("NexusModID", "818")]

namespace Backpack;

public class BackpackMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Configuration.Instance.Load();
        Logger.Info("Backpack initialized.");
    }
}