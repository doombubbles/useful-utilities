using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class QuickSell : ToggleableUtility
{
    public static bool blockQuickSell;

    protected override bool DefaultEnabled => true;
    public override string Description =>
        """
        If you hold down the Sell Tower hotkey, you will sell towers as you select them.
        Additionally, when you do click the Sell button in the Tower Selection Menu UI, using modifier keys will make it sell other others of that type
        Shift: Sell others of the same exact crosspath
        Control: Sell others of the same tier
        Alt: Sell others of the same tier or less
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

    [HarmonyPatch(typeof(InGame), nameof(InGame.SellTower))]
    internal static class InGame_SellTower
    {
        [HarmonyPrefix]
        internal static void Prefix(InGame __instance, TowerToSimulation tower)
        {
            if (blockQuickSell) return;

            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (!(shift || ctrl || alt)) return;

            foreach (var t in __instance.bridge.Simulation.towerManager.GetTowersByBaseId(tower.Def.baseId)
                         .ToList().Where(t => t.Id != tower.Id))
            {
                if (alt)
                {
                    if (t.towerModel.tier > tower.Def.tier) continue;
                }
                else if (ctrl)
                {
                    if (t.towerModel.tier != tower.Def.tier) continue;
                }
                else if (shift)
                {
                    if (!t.towerModel.CheckTiers(tower.Def.tiers, true, false)) continue;
                }

                __instance.bridge.simulation.SellTower(t, tower.owner);
            }
        }
    }
}