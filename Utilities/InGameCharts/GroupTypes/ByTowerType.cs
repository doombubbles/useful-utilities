using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.GroupTypes;

public class ByTowerType : GroupType
{
    protected override int Order => 3;

    public override string GroupId(TowerToSimulation tower) => tower.Def.baseId;

    public override BarInfo BarInfo(TowerToSimulation tower, bool hideMonkeyNames) => new()
    {
        Label = tower.Def.baseId.Localize() + (tower.hero == null ? "s" : ""),
        Icon = tower.Def.icon?.AssetGUID,
        Color = ColorForTowerSet(tower.TowerSet())
    };
}