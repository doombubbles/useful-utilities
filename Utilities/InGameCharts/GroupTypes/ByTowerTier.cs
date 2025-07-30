using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.GroupTypes;

public class ByTowerTier : GroupType
{
    public override string DisplayName => "By Tower+Tier";

    protected override int Order => 1;

    public override string GroupId(TowerToSimulation tower) => $"{tower.Def.baseId} {tower.Def.tier}";

    public override BarInfo BarInfo(TowerToSimulation tower, bool hideMonkeyNames) => new()
    {
        Label =
            $"{(tower.IsParagon ? "Paragon" : $"{(tower.hero != null ? "Level" : "Tier")} {tower.Def.tier}")} {tower.Def.baseId.Localize()}" +
            (tower.hero == null  ? "s" : ""),
        Icon = tower.Def.icon?.AssetGUID,
        Color = ColorForTower(tower),
    };
}