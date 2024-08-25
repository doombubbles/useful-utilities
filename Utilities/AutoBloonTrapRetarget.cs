using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers.Weapons;

namespace UsefulUtilities.Utilities;

public class AutoBloonTrapRetarget : UsefulUtility
{
    private enum TrapRetargetSetting
    {
        Off,
        StartOfRound,
        Constantly
    }

    private static readonly ModSettingEnum<TrapRetargetSetting> AutoTrapRetarget = new(TrapRetargetSetting.Off)
    {
        icon = VanillaSprites.BloonTrapUpgradeIcon,
        description = "For Engineers with with Bloon Trap and Larger Service Area, " +
                      "traps will now be automatically retargeted on the same spot to refresh them."
    };

    [HarmonyPatch(typeof(Simulation), nameof(Simulation.RoundStart))]
    internal static class Simulation_RoundStart
    {
        [HarmonyPostfix]
        internal static void Postfix(Simulation __instance)
        {
            if (AutoTrapRetarget != TrapRetargetSetting.StartOfRound) return;
            
            __instance.factory.GetUncast<TargetSelectedPoint>().ForEach(point =>
            {
                if (point.attack.model.name.Contains("BloonTrap") && point.HasValidPoint)
                {
                    point.ApplyTargetTypeData(point.targetPoint.ToVector2());
                }
            });
        }
    }


    [HarmonyPatch(typeof(Weapon), nameof(Weapon.Process))]
    internal static class Weapon_Process
    {
        [HarmonyPostfix]
        internal static void Postfix(Weapon __instance)
        {
            if (AutoTrapRetarget != TrapRetargetSetting.Constantly ||
                !__instance.attack.model.name.Contains("BloonTrap") ||
                !__instance.attack.activeTargetSupplier.Is(out TargetSelectedPoint targetSelectedPoint)) return;

            if (__instance.IsReloadReady && targetSelectedPoint.HasValidPoint)
            {
                targetSelectedPoint.ApplyTargetTypeData(targetSelectedPoint.targetPoint.ToVector2());
            }
        }
    }
}