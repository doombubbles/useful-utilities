#if DEBUG
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.Races;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class SandboxBosses : ToggleableUtility
{
    protected override ModSettingCategory Category => UsefulUtilitiesMod.Sandbox;

    protected override bool DefaultEnabled => false;

    public override string Description => "Adds Bosses and Boss related Bloons to the Sandbox Bloons menu";

    protected override string Icon => VanillaSprites.BossChallenge;

    protected override string DisableIfModPresent => "BossUIinSandbox";

    /// <summary>
    /// Initialize the Boss UI in Sandbox games
    /// </summary>
    [HarmonyPatch(typeof(InGame), nameof(InGame.StartMatch))]
    internal static class InGame_StartMatch
    {
        [HarmonyPostfix]
        internal static void Postfix(InGame __instance)
        {
            if (!GetInstance<SandboxBosses>().Enabled || !__instance.bridge.IsSandboxMode()) return;

            __instance.InstantiateUiObject("BossUI", new System.Action<GameObject>(o =>
            {
                o.GetComponent<BossUI>().Initialise().StartCoroutine();
            })).StartCoroutine();
        }
    }

    /// <summary>
    /// Fix this method not falling back to just checking the simulation for a Boss like GetBossBloonTTS does
    /// </summary>
    [HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.GetBossBloon))]
    internal static class UnityToSimulation_GetBossBloon
    {
        [HarmonyPostfix]
        private static void Postfix(UnityToSimulation __instance, ref Bloon? __result)
        {
            if (GetInstance<SandboxBosses>().Enabled && __instance.IsSandboxMode())
            {
                __result ??= __instance.GetBossBloonTTS()?.GetSimBloon();
            }
        }
    }

    /// <summary>
    /// Add Bosses and Boss related Bloons to the sandbox bloons menu
    /// </summary>
    [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.CreateBloonButtons))]
    public class BloonMenu_CreateBloonButtons
    {
        [HarmonyPrefix]
        public static void Prefix(Il2CppSystem.Collections.Generic.List<BloonModel> sortedBloons)
        {
            if (!GetInstance<SandboxBosses>().Enabled) return;

            foreach (var bloon in InGame.Bridge.Model.bloons)
            {
                if (bloon.HasTag(BloonTag.Boss) && !sortedBloons.Contains(bloon))
                {
                    sortedBloons.Add(bloon);
                }
            }
        }
    }
}
#endif