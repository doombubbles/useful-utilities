using System.Collections.Generic;
using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.StatTypes;

public class DamagePerRound : StatType
{
    public override string DisplayName => "Dmg / Round";

    protected override int Order => 1;

    public override string Icon => VanillaSprites.PopIcon;

    public override double Calculate(TowerToSimulation tower) =>
        tower.damageDealt - Meters.DamageRoundData.GetValueOrDefault(tower.Id, 0);

}