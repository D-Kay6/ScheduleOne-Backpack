using Il2CppInterop.Runtime.Injection;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMod), "BackpackMod", "1.4.0", "Tugakit", "https://www.nexusmods.com/schedule1/mods/107")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod;

public class BackpackMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        ClassInjector.RegisterTypeInIl2Cpp<BackpackStorage>(new RegisterTypeOptions() {Interfaces = new Il2CppInterfaceCollection([typeof(IItemSlotOwner)])});
        Melon<BackpackMod>.Logger.Msg("BackpackMod initialized.");
    }
}