using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.GroupTypes;

public class ByTowerCrosspath : GroupType
{
    public override string DisplayName => "By Tower+Path";

    protected override int Order => 2;

    public override string GroupId(TowerToSimulation tower) => $"{tower.Def.baseId} {tower.Def.tiers.Join()}";

    public override BarInfo BarInfo(TowerToSimulation tower, bool hideMonkeyNames) => new()
    {
        Label = GetTowerName(tower) + (tower.hero == null ? "s" : ""),
        Icon = tower.Def.tier >= 3 ? tower.Def.portrait?.AssetGUID : tower.Def.icon?.AssetGUID,
        Color = ColorForTower(tower)
    };

}