using System;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppNinjaKiwi.Common;
using UnityEngine;
using Vector3 = Il2CppAssets.Scripts.Simulation.SMath.Vector3;

namespace UsefulUtilities.Utilities;

public class CopyPasteTowers : UsefulUtility
{
    private static TowerModel? clipboard;
    private static double cost;
    private static int payForIt;
    private static bool justPastedTower;
    private static bool lastCopyWasCut;
    private static TargetType? targetType;

    private static readonly ModSettingHotkey CopyTower = new(KeyCode.C, HotkeyModifier.Ctrl);
    private static readonly ModSettingHotkey PasteTower = new(KeyCode.V, HotkeyModifier.Ctrl);
    private static readonly ModSettingHotkey CutTower = new(KeyCode.X, HotkeyModifier.Ctrl);
    protected override bool CreateCategory => true;

    public override void OnUpdate()
    {
        if (!InGame.instance) return;

        if (TowerSelectionMenu.instance)
        {
            var selectedTower = TowerSelectionMenu.instance.selectedTower;
            if (selectedTower is { IsParagon: false } && !selectedTower.tower.towerModel.IsHero())
            {
                lastCopyWasCut = CutTower.JustPressed();
                if (CutTower.JustPressed() || CopyTower.JustPressed())
                {
                    Copy(selectedTower.tower);

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
            justPastedTower && (PasteTower.IsPressed() || Input.GetKey(KeyCode.LeftShift)))
        {
            Paste();
        }

        justPastedTower = false;
        if (--payForIt < 0) payForIt = 0;
    }

    private static void Copy(Tower tower)
    {
        var inGameModel = InGame.instance.GetGameModel();
        clipboard = inGameModel.GetTowerWithName(tower.towerModel.name);

        cost = CalculateCost(clipboard);

        var name = LocalizationManager.Instance.GetText(tower.towerModel.name);
        Game.instance.ShowMessage($"Copied {name}\n\nTotal Cost is ${(int) cost}");

        targetType = tower.TargetType;
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
                ModHelper.Error<UsefulUtilitiesMod>(e);
            }
        }), new ObjectId { data = (uint) InGame.instance.UnityToSimulation.GetInputId() });
    }

    private static double CalculateCost(TowerModel towerModel, Vector3 pos = default)
    {
        var inGameModel = InGame.instance.GetGameModel();
        var towerManager = InGame.instance.GetTowerManager();

        var total = 0.0;

        var discountMult = 0f;
        if (pos != default)
        {
            var zoneDiscount = towerManager.GetZoneDiscount(pos, 0, 0);
            discountMult = towerManager.GetDiscountMultiplier(zoneDiscount);
        }

        total += (1 - discountMult) * towerModel.cost;

        foreach (var appliedUpgrade in towerModel.appliedUpgrades)
        {
            var upgrade = inGameModel.GetUpgrade(appliedUpgrade);

            discountMult = 0f;
            if (pos != default)
            {
                var zoneDiscount = towerManager.GetZoneDiscount(pos, upgrade.path, upgrade.tier);
                discountMult = towerManager.GetDiscountMultiplier(zoneDiscount);
            }

            total += upgrade.cost * (1 - discountMult);
        }

        return total;
    }

    private static double CalculateCost(Tower tower) => CalculateCost(tower.towerModel, tower.Position);

    [HarmonyPatch(typeof(Tower), nameof(Tower.OnPlace))]
    private static class Tower_OnPlace
    {
        [HarmonyPrefix]
        private static void Prefix(Tower __instance)
        {
            if (payForIt <= 0) return;

            __instance.worth = (float) CalculateCost(__instance);
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
        }
    }
}