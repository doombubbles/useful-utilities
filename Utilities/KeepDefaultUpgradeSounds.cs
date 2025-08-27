using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Cosmetics;
using Il2CppAssets.Scripts.Data.Cosmetics.TowerAssetChanges;
using Il2CppAssets.Scripts.Data.TrophyStore;
using Il2CppSystem.Collections.Generic;

namespace UsefulUtilities.Utilities;

public class KeepDefaultUpgradeSounds : ToggleableUtility
{
    private static readonly Dictionary<string, List<AudioSwap>> AudioSwaps = new();

    protected override bool DefaultEnabled => false;
    protected override ModSettingCategory Category => UsefulUtilitiesMod.TrophyStore;
    protected override string Icon => VanillaSprites.FireworksUpgradeFxIcon;

    public override string Description => "Makes Trophy Store upgrade effects not apply their sound changes.";

    private static bool IsEnabled => GetInstance<KeepDefaultUpgradeSounds>().Enabled;

    public override void OnSaveSettings() => ModifyItems();

    public override void OnTitleScreen() => ModifyItems();

    public static void ModifyItems()
    {
        var trophyStoreItems = GameData.Instance.trophyStoreItems;

        var assetChanges = trophyStoreItems.storeItems.ToList().SelectMany(item => item.itemTypes)
            .Where(data => data.itemType == TrophyItemType.TowerAssetChange &&
                           data.itemTarget.name.EndsWith("UpgradeEffect"))
            .Select(data => data.itemTarget.Cast<TowerAssetChange>());

        foreach (var assetChange in assetChanges)
        {
            AudioSwaps.TryAdd(assetChange.id, assetChange.audioSwaps);

            assetChange.audioSwaps = IsEnabled ? new List<AudioSwap>() : AudioSwaps[assetChange.id];
        }
    }
}