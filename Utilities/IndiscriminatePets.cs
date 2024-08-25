using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Cosmetics.Pets;
using Il2CppAssets.Scripts.Data.TrophyStore;

#if USEFUL_UTILITIES
namespace UsefulUtilities.Utilities;
#else
namespace IndiscriminatePets;
#endif

#if USEFUL_UTILITIES
public class IndiscriminatePets : ToggleableUtility
#else
public class IndiscriminatePetsUtility
#endif
{
#if USEFUL_UTILITIES
    protected override bool DefaultEnabled => true;

    protected override ModSettingCategory Category => UsefulUtilitiesMod.TrophyStore;

    public override string Description => "Allows trophy store pets to apply to all skins of the hero they're for.";

    private static bool IsEnabled => GetInstance<IndiscriminatePets>().Enabled;

    public override void OnSaveSettings() => ModifyItems();
#else
    private const bool IsEnabled = true;
#endif

    private static readonly Dictionary<string, string> PetSkinIds = new();

    public static void ModifyItems()
    {
        var trophyStoreItems = GameData.Instance.trophyStoreItems;

        var pets = trophyStoreItems.storeItems.ToList()
            .SelectMany(item => item.itemTypes)
            .Where(data => data.itemType == TrophyItemType.TowerPet)
            .Select(data => data.itemTarget.Cast<Pet>());

        foreach (var pet in pets)
        {
            PetSkinIds.TryAdd(pet.id, pet.skinId);

            pet.skinId = IsEnabled ? "" : PetSkinIds[pet.id];
        }
    }
}