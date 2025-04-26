using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMod), "BackpackMod", "1.5.1", "D-Kay", "https://www.nexusmods.com/schedule1/mods/818")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: AssemblyMetadata("NexusModID", "818")]

namespace BackpackMod;

public class BackpackMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Melon<BackpackMod>.Logger.Msg("BackpackMod initialized.");
    }
}