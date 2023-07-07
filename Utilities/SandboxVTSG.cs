using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;

namespace UsefulUtilities.Utilities;

public class SandboxVTSG : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Allows the Vengeful True Sun God to be formed in Sandbox Mode";

    public override string DisplayName => "Sandbox VTSG";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Sandbox;

    [HarmonyPatch(typeof(MonkeyTemple), nameof(MonkeyTemple.StartSacrifice))]
    internal static class MonkeyTemple_StartSacrifice
    {
        [HarmonyPrefix]
        private static void Prefix(MonkeyTemple __instance, ref string __state)
        {
            __state = __instance.Sim.model.gameMode;
            if (GetInstance<SandboxVTSG>().Enabled && __state == "Sandbox")
            {
                __state = "";
            }
        }

        [HarmonyPostfix]
        private static void Postfix(MonkeyTemple __instance, ref string __state)
        {
            if (GetInstance<SandboxVTSG>().Enabled)
            {
                __instance.Sim.model.gameMode = __state;
            }
        }
    }
}