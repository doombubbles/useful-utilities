using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class QuickSell : ToggleableUtility
{
    protected override bool DefaultEnabled => true;
    public override string Description =>
        """
        If you hold down the Sell Tower hotkey, you will sell towers as you select them.
        If you hold Ctrl+Shift while selling, you will sell all towers of the exact same type and tiers.
        If you also hold Alt, it will include all towers of that type that are that tier or lower.
        """;
    protected override string Icon => VanillaSprites.MoneyBag;

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.SelectTower))]
    internal static class TowerSelectionMenu_SelectTower
    {
        [HarmonyPostfix]
        internal static bool Prefix(TowerToSimulation tower)
        {
            if (!InGame.instance || InGame.Bridge == null || InGame.instance.ReviewMapMode ||
                InGame.Bridge.IsSpectatorMode) return true;

            if (!InGame.instance.hotkeys.sell.isPressed || !GetInstance<QuickSell>().Enabled) return true;

            InGame.instance.SellTower(tower);
            return false;
        }
    }


    /// <summary>
    /// TODO make this work bc of hotkey stuff
    ///
    /// </summary>
    [HarmonyPatch(typeof(InGame), nameof(InGame.SellTower))]
    internal static class InGame_SellTower
    {
        private static bool active;

        [HarmonyPostfix]
        internal static void Postfix(InGame __instance, TowerToSimulation tower)
        {
            if (active || !((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))) return;

            active = true;

            try
            {
                foreach (var t in __instance.bridge.Simulation.towerManager.GetTowersByBaseId(tower.Def.baseId)
                             .ToList())
                {
                    if (t.Id == tower.Id) continue;

                    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        if (t.towerModel.tier > tower.Def.tier) continue;
                    }
                    else
                    {
                        if (!t.towerModel.CheckTiers(tower.Def.tiers, true, false)) continue;
                    }

                    __instance.SellTower(tower);
                }
            }
            finally
            {
                active = false;
            }
        }
    }
}