using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Cosmetics.Pets;
using Il2CppAssets.Scripts.Data.TrophyStore;

namespace UsefulUtilities.Utilities;

public class IndiscriminatePets : ToggleableUtility
{
    private static readonly Dictionary<string, string> PetSkinIds = new();
    
    protected override bool DefaultEnabled => true;

    protected override ModSettingCategory Category => UsefulUtilitiesMod.TrophyStore;

    public override string Description => "Allows trophy store pets to apply to all skins of the hero they're for.";

    public override void OnSaveSettings()
    {
        var trophyStoreItems = GameData.Instance.trophyStoreItems;

        var pets = trophyStoreItems.storeItems.ToList()
            .SelectMany(item => item.itemTypes)
            .Where(data => data.itemType == TrophyItemType.TowerPet)
            .Select(data => data.itemTarget.Cast<Pet>());

        foreach (var pet in pets)
        {
            PetSkinIds.TryAdd(pet.id, pet.skinId);

            pet.skinId = Enabled ? "" : PetSkinIds[pet.id];
        }
    }
}