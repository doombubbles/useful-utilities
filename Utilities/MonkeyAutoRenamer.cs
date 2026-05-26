using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppSystem.IO;

namespace UsefulUtilities.Utilities;

public class MonkeyAutoRenamer : ToggleableUtility
{
    protected override bool DefaultEnabled => false;

    public override string Description =>
        "When a named monkey is sold/destroyed, the monkey with the next highest number of pops will carry on its name.";

    protected override string Icon => VanillaSprites.NamedMonkeyIcon;

    [HarmonyPatch(typeof(Tower), nameof(Tower.OnDestroy))]
    internal static class Tower_Destroy
    {
        [HarmonyPrefix]
        internal static void Prefix(Tower __instance)
        {
            if (string.IsNullOrEmpty(__instance.namedMonkeyKey) || !GetInstance<MonkeyAutoRenamer>().Enabled) return;

            var newTower = __instance.Sim.towerManager.GetTowersByBaseId(__instance.towerModel.baseId)
                .ToArray()
                .OrderByDescending(tower => tower.damageDealt)
                .ThenByDescending(tower => tower.cashEarned)
                .FirstOrDefault();

            newTower?.namedMonkeyKey = __instance.namedMonkeyKey;
        }
    }
}