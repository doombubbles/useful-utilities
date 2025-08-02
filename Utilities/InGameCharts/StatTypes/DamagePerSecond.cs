using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

namespace UsefulUtilities.Utilities.InGameCharts.StatTypes;

public class DamagePerSecond : StatType
{
    public override string DisplayName => "Dmg / Sec";

    protected override int Order => 2;

    public override string Icon => VanillaSprites.PopIcon;

    public const float UpdatesPerSecond = 60f;

    public override double Calculate(TowerToSimulation tower)
    {
        var entry = Meters.DamageTimeData.FirstOrDefault(tuple => tuple.Item2.ContainsKey(tower.Id));
        if (entry == default) return 0;

        var (time, damage) = entry;
        var now = InGame.Bridge.Simulation.roundTime.elapsed;

        if (now <= time) return 0;

        return UpdatesPerSecond * (tower.damageDealt - damage[tower.Id]) / (now - time);
    }
}