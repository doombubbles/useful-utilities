using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Track;
using Il2CppAssets.Scripts.Utils.Messaging;
namespace UsefulUtilities.Utilities;

public class SandboxRoundEnd : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Allows rounds to properly end in sandbox mode";

    protected override string Icon => VanillaSprites.SandboxBtn;

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Sandbox;

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.CheckForRoundEnd))]
    internal static class Spawner_CheckForRoundEnd
    {
        [HarmonyPrefix]
        internal static void Prefix(Spawner __instance, ref bool __state)
        {
            if (!GetInstance<SandboxRoundEnd>().Enabled) return;
            __state = __instance.isSandbox;
            __instance.isSandbox = false;
        }

        [HarmonyPostfix]
        internal static void Postfix(Spawner __instance, bool __state)
        {
            if (!GetInstance<SandboxRoundEnd>().Enabled) return;
            __instance.isSandbox = __state;
        }
    }

    [HarmonyPatch(typeof(Simulation), nameof(Simulation.RoundEnd))]
    internal static class Simulation_RoundEnd
    {
        [HarmonyPrefix]
        internal static bool Prefix(Simulation __instance, int round, int highestCompletedRound)
        {
            if (!__instance.sandbox) return true;

            __instance.DistributeXp(round);
            __instance.factory.GetUncast<Tower>().ForEach(tower => tower.OnRoundComplete(round));
            __instance.OnRoundEndProjectiles();

            BridgeMessaging<BridgeMessagingDelegates.OnEarlyRoundEnd>.Trigger?.Invoke(round);
            foreach (var simulationBehavior in __instance.behaviorCache)
            {
                simulationBehavior.OnRoundEnd(round);
            }
            BridgeMessaging<BridgeMessagingDelegates.OnRoundEnd>.Trigger?.Invoke(round);
            BridgeMessaging<BridgeMessagingDelegates.OnLateRoundEnd>.Trigger?.Invoke(round, highestCompletedRound);
            BridgeMessaging<BridgeMessagingDelegates.OnSafeToResync>.Trigger?.Invoke(highestCompletedRound);

            return false;
        }
    }
}