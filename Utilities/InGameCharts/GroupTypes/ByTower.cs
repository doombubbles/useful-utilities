using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.GroupTypes;

public class ByTower : GroupType
{
    protected override int Order => 0;

    public override string GroupId(TowerToSimulation tower) => tower.tower.uniqueId;

    public override BarInfo BarInfo(TowerToSimulation tower, bool hideMonkeyNames) => new()
    {
        Label = GetTowerName(tower, hideMonkeyNames),
        Icon = tower.Def.portrait?.AssetGUID,
        Color = ColorForTower(tower)
    };
}