using System.Reflection;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne;
using Backpack.Config;
using MelonLoader;

[assembly: MelonInfo(typeof(Backpack.BackpackMod), "OG Backpack", "1.8.0", "D-Kay", "https://www.nexusmods.com/schedule1/mods/818")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: AssemblyMetadata("NexusModID", "818")]

namespace Backpack;

public class BackpackMod : MelonMod
{
    public readonly static List<Backpack> Backpacks =
    [
        new Backpack("Small Backpack", "A small backpack for minimal items.", 4, 2000, new FullRank(ERank.Hoodlum, 1), "small.png"),
        new Backpack("Medium Backpack", "A very standard backpack for various items.", 8, 4000, new FullRank(ERank.Hustler, 2), "medium.png"),
        new Backpack("Large Backpack", "A large backpack for big guns and items.", 20, 8000, new FullRank(ERank.Enforcer, 1), "big.png"),
    ];

    private static ShopManager _shopManager;

    public static ShopManager ShopManager
    {
        get => _shopManager;
        private set => _shopManager = value;
    }

    public override void OnInitializeMelon()
    {
        Configuration.Instance.Load();
        Configuration.Instance.Save(); // Save the default config to force the creation of the config file
        Logger.Info("Backpack initialized.");
    }

    public static void InitBackpacks()
    {
        foreach (var backpack in Backpacks)
        {
            backpack.ItemDefinition = new StorableItemDefinition
            {
                ID = backpack.Name,
                Name = backpack.Name,
                Description = backpack.Description,
                BasePurchasePrice = backpack.Price,
                Category = EItemCategory.Tools,
                Icon = backpack.Icon,
                StackLimit = 1,
                RequiresLevelToPurchase = true,
                RequiredRank = backpack.FullRank,

            };

            backpack.ItemInstance = new ItemInstance(backpack.ItemDefinition, 1)
            {
                ID = backpack.Name,
            };

            backpack.ShopListing = new BackpackListing(backpack);

            Registry.Instance.AddToRegistry(backpack.ItemDefinition);

            Logger.Info($"Backpack '{backpack.Name}' initialized with ID: {backpack.ItemDefinition.ID}");
        }

        ShopManager = new ShopManager();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Configuration.Instance.Reset();
        if (sceneName != "Main")
            return;

        ConfigSyncManager.StartSync();
    }
}
