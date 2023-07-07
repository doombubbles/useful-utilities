using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Towers;

namespace UsefulUtilities.Utilities;

public class SandboxParagons : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Allows Paragons to be made in Sandbox Mode";

    protected override string DisableIfModPresent => "Unlimited5thTiers";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Sandbox;

    [HarmonyPatch(typeof(Tower), nameof(Tower.CanUpgradeToParagon))]
    internal class Tower_CanUpgradeToParagon
    {
        [HarmonyPrefix]
        private static void Prefix(Tower __instance, ref bool __state)
        {
            __state = __instance.Sim.sandbox;
            if (GetInstance<SandboxParagons>().Enabled)
            {
                __instance.Sim.sandbox = false;
            }
        }

        [HarmonyPostfix]
        internal static void Postfix(Tower __instance, ref bool __result, ref bool __state)
        {
            if (GetInstance<SandboxParagons>().Enabled)
            {
                __instance.Sim.sandbox = __state;
            }
        }
    }
}