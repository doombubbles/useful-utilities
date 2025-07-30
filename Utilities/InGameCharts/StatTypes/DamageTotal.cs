using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.StatTypes;

public class DamageTotal : StatType
{
    public override string DisplayName => "Dmg Total";

    protected override int Order => 0;

    public override string Icon => VanillaSprites.PopIcon;

    public override double Calculate(TowerToSimulation tower) => tower.damageDealt;
}