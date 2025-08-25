using System;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppNinjaKiwi.Common;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using Math = Il2CppAssets.Scripts.Simulation.SMath.Math;
using Vector3 = Il2CppAssets.Scripts.Simulation.SMath.Vector3;

#if USEFUL_UTILITIES
namespace UsefulUtilities.Utilities;
#else
using static CopyPasteTowers.CopyPasteTowersMod;
namespace CopyPasteTowers;
#endif

#if USEFUL_UTILITIES
public class CopyPasteTowers : UsefulUtility
#else
public class CopyPasteTowersUtility
#endif
{
#if USEFUL_UTILITIES
    private static readonly ModSettingHotkey CopyTower = new(KeyCode.C, HotkeyModifier.Ctrl);
    private static readonly ModSettingHotkey PasteTower = new(KeyCode.V, HotkeyModifier.Ctrl);
    private static readonly ModSettingHotkey CutTower = new(KeyCode.X, HotkeyModifier.Ctrl);
    protected override bool CreateCategory => true;

    public override void OnUpdate() => Update();
#endif

    private static TowerModel? clipboard;
    private static double cost;
    private static int payForIt;
    private static bool justPastedTower;
    private static bool lastCopyWasCut;
    private static TargetType? targetType;
    private static ParagonTower.InvestmentInfo? lastDegree;

    public static void Update()
    {
        if (!InGame.instance ||
            InGame.Bridge == null ||
            InGame.instance.ReviewMapMode ||
            InGame.Bridge.IsSpectatorMode ||
            InGame.instance.GameType == GameType.Rogue) return;

        if (TowerSelectionMenu.instance)
        {
            var selectedTower = TowerSelectionMenu.instance.selectedTower;
            var tower = selectedTower?.Def;
            if (tower is {isSubTower: false} &&
                (ModHelper.HasMod("Unlimited5thTiers") || !tower.IsHero() && !tower.isParagon) &&
                tower.name.StartsWith(tower.baseId))
            {
                lastCopyWasCut = CutTower.JustPressed();
                if (CutTower.JustPressed() || CopyTower.JustPressed())
                {
                    Copy(selectedTower!.tower);

                    if (CutTower.JustPressed())
                    {
                        TowerSelectionMenu.instance.Sell();
                        lastCopyWasCut = true;
                    }
                    else
                    {
                        lastCopyWasCut = false;
                    }
                }
            }
        }

        if (PasteTower.JustPressed() ||
            justPastedTower
#if USEFUL_UTILITIES
            &&
            (PasteTower.IsPressed() || MultiPlace.MultiPlaceModifier.IsPressed())
#endif
           )
        {
            Paste();
        }

        justPastedTower = false;
        if (--payForIt < 0) payForIt = 0;
    }

    private static void Copy(Tower tower)
    {
        cost = CalculateBaseCost(tower);

        var inGameModel = InGame.instance.GetGameModel();
        clipboard = inGameModel.GetTowerWithName(tower.towerModel.name);

        foreach (var mod in ModHelper.Mods)
        {
            mod.Call("OnTowerCopied", tower);
        }

        var name = LocalizationManager.Instance.GetText(tower.towerModel.name);
        Game.instance.ShowMessage($"Copied {name}\n\nTotal Cost is ${(int) cost}");

        targetType = tower.TargetType;
        lastDegree = tower.towerModel.isParagon ? tower.entity.GetBehavior<ParagonTower>()?.investmentInfo : null;
    }

    private static void Paste()
    {
        var inputManager = InGame.instance.InputManager;
        if (clipboard == null || inputManager.IsInPlacementMode || InGame.instance.GetCash() < cost)
        {
            return;
        }

        inputManager.EnterPlacementMode(clipboard, new Action<Vector2>(pos =>
        {
            try
            {
                payForIt = 30;
                inputManager.CreatePlacementTower(pos);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e);
            }
        }), new ObjectId {data = (uint) InGame.instance.bridge.GetInputId()});
    }

    private static int ModifyClipboardCost(Tower tower) =>
        ModHelper.Mods.Aggregate(0, (i, mod) => i + Convert.ToInt32(mod.Call("ModifyClipboardCost", tower) ?? 0));

    private static int BaseUpgradeCost(string name) =>
        Math.RoundToNearestInt(InGame.instance.GetGameModel().GetUpgrade(name).cost, 5);

    private static double CalculateBaseCost(Tower tower) =>
        tower.towerModel.appliedUpgrades.Aggregate(tower.towerModel.cost, (i, upgrade) => i + BaseUpgradeCost(upgrade))
        + ModifyClipboardCost(tower);

    private static int CalculateCost(Tower tower)
    {
        var sim = tower.Sim;
        var tm = sim.towerManager;
        var ti = sim.GetTowerInventory(tower.PlayerOwnerId);

        var totalMult = 0f;
        var totalChange = 0f;
        ti.GetTowerDiscount(tower.towerModel, ref totalMult, ref totalChange, false);

        var areaDiscount = tm.GetAreaDiscount(tower.Position.ToVector2());
        var towerZoneDiscount = tm.GetZoneDiscount(tower.towerModel, tower.Position, -1, 0, tower.owner);
        var towerDiscountMult = tm.GetDiscountMultiplier(towerZoneDiscount);

        var total = Math.RoundToNearestInt(
            (1 - (areaDiscount + towerDiscountMult + totalMult)) * (tower.towerModel.cost - totalChange), 5);

        total += CalculateUpgradeCosts(tower);

        total += ModifyClipboardCost(tower);

        return total;
    }

    private static int CalculateUpgradeCosts(Tower tower) =>
        tower.towerModel.appliedUpgrades.Sum(u => CalculateCost(tower, tower.Sim.model.GetUpgrade(u)));

    private static int CalculateCost(Tower tower, UpgradeModel upgrade)
    {
        var sim = tower.Sim;
        var tm = sim.towerManager;

        var path = upgrade.path;
        var tier = upgrade.tier + 1;

        if (tm.GetFreeUpgrade(tower.Position, tower, path, tier)) return 0;

        var areaDiscount = tm.GetAreaDiscount(tower.Position.ToVector2());
        var upgradeZoneDiscount = tm.GetZoneDiscount(tower.towerModel, tower.Position, path, tier + 1, tower.owner);
        var upgradeDiscountMult = tm.GetDiscountMultiplier(upgradeZoneDiscount);

        var upgradeMult = sim.GetSimulationBehaviorDiscount(tower, path, tier, areaDiscount + upgradeDiscountMult);

        return Math.RoundToNearestInt(upgrade.cost * (1 - upgradeMult), 5);
    }

    [HarmonyPatch(typeof(Tower), nameof(Tower.OnPlace))]
    private static class Tower_OnPlace
    {
        [HarmonyPrefix]
        private static void Prefix(Tower __instance)
        {
            if (payForIt <= 0) return;

            foreach (var mod in ModHelper.Mods)
            {
                mod.Call("OnTowerPasted", __instance);
            }

            __instance.worth = CalculateCost(__instance);
            InGame.instance.AddCash(-__instance.worth + __instance.towerModel.cost);
            payForIt = 0;
            justPastedTower = true;

            if (lastCopyWasCut)
            {
                var tts = __instance.GetTowerToSim();
                TowerSelectionMenu.instance.SelectTower(tts);
            }

            if (targetType != null)
            {
                __instance.SetTargetType(targetType);
            }

            if (__instance.towerModel.isParagon && lastDegree != null)
            {
                var paragonTower = __instance.entity.GetBehavior<ParagonTower>();
                paragonTower.investmentInfo = lastDegree.Value;
                paragonTower.UpdateDegree();
                paragonTower.PlayParagonUpgradeSound();
                paragonTower.Finish();
            }
        }
    }

    /// <summary>
    /// Clear clipboard on Match Start, Restart, Continue, Exit
    /// </summary>
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.ResetNow))]
    internal static class TimeManager_ResetNow
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            clipboard = null;
            foreach (var mod in ModHelper.Mods)
            {
                mod.Call("OnClipboardCleared");
            }
        }
    }
}