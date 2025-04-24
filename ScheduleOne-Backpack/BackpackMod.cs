using MelonLoader;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMod), "BackpackMod", "1.5.0", "D-Kay")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod;

public class BackpackMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Melon<BackpackMod>.Logger.Msg("BackpackMod initialized.");
    }
}