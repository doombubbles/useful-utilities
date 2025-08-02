using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.StatTypes;

public class CashTotal : StatType
{
    public override string DisplayName => "Cash Total";

    protected override int Order => 3;

    public override string Icon => VanillaSprites.BananaIcon;

    public override double Calculate(TowerToSimulation tower) => tower.cashEarned;
}