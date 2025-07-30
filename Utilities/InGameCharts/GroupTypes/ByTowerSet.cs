using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.GroupTypes;

public class ByTowerSet : GroupType
{
    protected override int Order => 4;

    public override string GroupId(TowerToSimulation tower) => tower.TowerSet().ToString();

    public override BarInfo BarInfo(TowerToSimulation tower, bool hideMonkeyNames) => new()
    {
        Label = tower.TowerSet().ToString().Localize(),
        Color = ColorForTowerSet(tower.TowerSet()),
        Icon = tower.TowerSet() switch
        {
            TowerSet.Primary => VanillaSprites.TowerTypePrimary,
            TowerSet.Military => VanillaSprites.TowerTypeMilitary,
            TowerSet.Magic => VanillaSprites.TowerTypeMagic,
            TowerSet.Support => VanillaSprites.TowerTypeSupport,
            TowerSet.Hero => VanillaSprites.HeroIcon,
            _ => null
        }
    };
}