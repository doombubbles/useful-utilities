using System.Collections.Generic;
using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.StatTypes;

public class CashPerRound : StatType
{
    public override string DisplayName => "Cash / Round";

    protected override int Order => 4;

    public override string Icon => VanillaSprites.BananaIcon;

    public override double Calculate(TowerToSimulation tower) =>
        tower.cashEarned - Meters.CashRoundData.GetValueOrDefault(tower.Id, 0);
}