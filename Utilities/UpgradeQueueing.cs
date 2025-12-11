using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.Stats;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppInterop.Runtime.Attributes;
using Il2CppNinjaKiwi.Common;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using TaskScheduler = BTD_Mod_Helper.Api.TaskScheduler;

namespace UsefulUtilities.Utilities;

public class UpgradeQueueing : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "If you attempt to purchase an Upgrade when you don't have the cash for it, " +
        "it will be queued to automatically purchase once you do.";

    public record QueuedUpgrade(ObjectId TowerId, int Path, int Tier, string UpgradeId);
    public static readonly List<QueuedUpgrade> QueuedUpgrades = [];

    public static void EnqueueUpgrade(QueuedUpgrade queuedUpgrade)
    {
        ModHelper.Msg<UsefulUtilitiesMod>($"Queuing upgrade {queuedUpgrade.UpgradeId.Localize()}");
        QueuedUpgrades.Add(queuedUpgrade);
        OnQueueChanged();
    }

    public static void DequeueUpgrade(QueuedUpgrade queuedUpgrade)
    {
        ModHelper.Msg<UsefulUtilitiesMod>($"Canceling queued upgrade {queuedUpgrade.UpgradeId.Localize()}");

        QueuedUpgrades.Remove(queuedUpgrade);
        QueuedUpgrades.RemoveAll(upgrade =>
            upgrade.TowerId == queuedUpgrade.TowerId &&
            upgrade.Path == queuedUpgrade.Path &&
            upgrade.Tier > queuedUpgrade.Tier);
        OnQueueChanged();
    }

    public static void OnQueueChanged()
    {
        UpgradeQueue.Instance.Refresh(QueuedUpgrades);

        if (TowerSelectionMenu.instance.selectedTower != null)
        {
            foreach (var upgradeObject in TowerSelectionMenu.instance.upgradeButtons)
            {
                upgradeObject.UpdateVisuals(upgradeObject.path, false);
            }
        }
    }

    public override void OnUpdate()
    {
        if (!Enabled || InGame.instance == null || InGame.Bridge == null) return;

        // Make upgrade hotkeys work with queueing with Shift
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
            TowerSelectionMenu.instance?.selectedTower != null)
        {
            var hotkeys = InGame.instance.hotkeys;
            var pathHotkeys = new[]
                { hotkeys.upgradePath1Hotkey, hotkeys.upgradePath2Hotkey, hotkeys.upgradePath3Hotkey };
            for (var i = 0; i < pathHotkeys.Length; i++)
            {
                var hotkey = pathHotkeys[i];
                if (Input.GetKeyDown(hotkey.boundPath.GetKeyCode()))
                {
                    TowerSelectionMenu.instance.UpgradeTower(i);
                }
            }
        }

        var towerManager = InGame.Bridge.Simulation.towerManager;

        QueuedUpgrades.RemoveAll(upgrade =>
            towerManager.GetTowerById(upgrade.TowerId) is not { IsDestroyed: false } tower ||
            tower.towerModel.tiers[upgrade.Path] >= upgrade.Tier); // TODO support Paths++

        if (!QueuedUpgrades.Any()) return;

        var queuedUpgrade = QueuedUpgrades.First();

        var tower = towerManager.GetTowerById(queuedUpgrade.TowerId);
        var cost = 99999999f;

        if (towerManager.CanUpgradeTower(tower, queuedUpgrade.Path, queuedUpgrade.Tier, tower.PlayerOwnerId,
                ref cost))
        {
            var towerModel =
                InGame.Bridge.Model.GetTowerFromId(GetUpgrade(tower.towerModel, queuedUpgrade.Path)!.tower);

            QueuedUpgrades.Remove(queuedUpgrade);

            towerManager.UpgradeTower(tower.PlayerOwnerId, tower, towerModel, queuedUpgrade.Path, cost);
        }

    }

    // TODO support Paths++
    private static UpgradePathModel? GetUpgrade(TowerModel towerModel, int path) => towerModel.upgrades
        .FirstOrDefault(u => InGame.Bridge.Model.GetUpgrade(u.upgrade).path == path);

    // TODO delay after upgrading
    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpgradeTower), typeof(int), typeof(bool))]
    internal static class TowerSelectionMenu_UpgradeTower
    {
        [HarmonyPrefix]
        internal static bool Prefix(TowerSelectionMenu __instance, int index, bool isParagon)
        {
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!GetInstance<UpgradeQueueing>().Enabled || isParagon) return true;

            var tower = __instance.selectedTower;

            var cost = tower.GetUpgradeCost(index, __instance.upgradeButtons[index]!.tier + 1, -1, isParagon);

            if (__instance.Bridge.GetCash() >= cost && !shift) return true;

            var queuedUpgradesForTower = QueuedUpgrades
                .Where(upgrade => upgrade.TowerId == tower.Id)
                .ToArray();

            var gameModel = __instance.Bridge.Model;
            var towerModel = tower.Def;
            foreach (var queuedUpgrade in queuedUpgradesForTower)
            {
                var u = GetUpgrade(towerModel, queuedUpgrade.Path);
                if (u == null) return !shift;
                towerModel = gameModel.GetTowerFromId(u.tower);
            }

            var upgrade = GetUpgrade(towerModel, index);
            if (upgrade == null) return !shift;

            var tier = towerModel.tiers[index] + 1;

            if (!__instance.Bridge.IsUpgradeLocked(tower.Id, index, tier))
            {
                EnqueueUpgrade(new QueuedUpgrade(tower.Id, index, tier, upgrade.upgrade));
            }

            return !shift;
        }
    }

    [HarmonyPatch(typeof(CashDisplay), nameof(CashDisplay.Initialise))]
    internal static class CashDisplay_Initialise
    {
        [HarmonyPrefix]
        internal static void Prefix(CashDisplay __instance)
        {
            if (!GetInstance<UpgradeQueueing>().Enabled) return;

            UpgradeQueue.Create(__instance.transform);
        }
    }

    [HarmonyPatch(typeof(CashDisplay), nameof(CashDisplay.OnCashChanged))]
    internal static class CashDisplay_OnCashChanged
    {
        [HarmonyPostfix]
        internal static void Postfix(CashDisplay __instance)
        {
            if (!GetInstance<UpgradeQueueing>().Enabled) return;

            var upgradeQueue = __instance.GetComponentInChildren<UpgradeQueue>();
            if (upgradeQueue == null) return;
            upgradeQueue.Refresh(QueuedUpgrades);
            // upgradeQueue.transform.localPosition = new Vector3(__instance.text.preferredWidth, 0, 0) + upgradeQueue.offset;
        }
    }

    [RegisterTypeInIl2Cpp(false)]
    internal class UpgradeQueue(IntPtr ptr) : MonoBehaviour(ptr)
    {
        public static UpgradeQueue Instance { get; private set; }
        public ModHelperScrollPanel scrollPanel;
        // public Vector3 offset = new(-250, -10, 0);

        public readonly Il2CppSystem.Collections.Generic.List<QueuedUpgradeIcon> allIcons = new();

        [HideFromIl2Cpp]
        public void Refresh(IEnumerable<QueuedUpgrade> queuedUpgrades)
        {
            var upgrades = queuedUpgrades.ToArray();
            while (upgrades.Length >= allIcons.Count)
            {
                var newIcon = QueuedUpgradeIcon.Create(scrollPanel);
                allIcons.Add(newIcon);
            }

            for (var i = 0; i < upgrades.Length; i++)
            {
                var icon = allIcons[i];
                icon.gameObject.SetActive(true);
                icon.SetUpgrade(upgrades[i]);
            }
            for (var i = upgrades.Length; i < allIcons.Count; i++)
            {
                allIcons[i].gameObject.SetActive(false);
            }
        }

        public static UpgradeQueue Create(Transform parent)
        {
            var queue = parent.gameObject.AddModHelperScrollPanel(new Info("QueuedUpgrades")
            {
                Height = 100, Width = 550, Pivot = new Vector2(0, 0.5f), Anchor = new Vector2(0, 0.5f), Y = -125
            }, RectTransform.Axis.Horizontal, "", 25f);
            queue.ScrollContent.RectTransform.pivot = new Vector2(0, 0.5f);
            queue.Mask.showMaskGraphic = false;
            queue.Mask.graphic.raycastTarget = false;

            var upgradeQueue = queue.AddComponent<UpgradeQueue>();
            upgradeQueue.scrollPanel = queue;

            Instance = upgradeQueue;

            return upgradeQueue;
        }
    }

    [RegisterTypeInIl2Cpp(false)]
    internal class QueuedUpgradeIcon(IntPtr ptr) : MonoBehaviour(ptr)
    {
        public ModHelperButton button;
        public QueuedUpgrade? upgrade;

        [HideFromIl2Cpp]
        public void SetUpgrade(QueuedUpgrade queuedUpgrade)
        {
            upgrade = queuedUpgrade;
            var upgradeModel = InGame.Bridge.Model.GetUpgrade(queuedUpgrade.UpgradeId);
            button.Image.LoadSprite(upgradeModel.icon);
        }

        public static QueuedUpgradeIcon Create(ModHelperScrollPanel parent)
        {
            var button = ModHelperButton.Create(new Info("QueuedUpgrade", 100), "", null);
            var queuedUpgradeIcon = button.AddComponent<QueuedUpgradeIcon>();
            queuedUpgradeIcon.button = button;

            button.Button.SetOnClick(() =>
            {
                if (queuedUpgradeIcon.upgrade != null)
                {
                    DequeueUpgrade(queuedUpgradeIcon.upgrade);
                }
            });

            parent.AddScrollContent(button);

            return queuedUpgradeIcon;
        }
    }

    [HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.UpdateVisuals))]
    internal static class UpgradeObject_UpdateVisuals
    {
        [HarmonyPostfix]
        internal static void Postfix(UpgradeObject __instance)
        {
            if (!GetInstance<UpgradeQueueing>().Enabled) return;

            for (var i = 0; i < __instance.tiers.Count; i++)
            {
                var tierObject = __instance.tiers[i]!;
                var tier = i + 1;

                var index = QueuedUpgrades.FindIndex(upgrade =>
                    upgrade.TowerId == __instance.tts.Id && upgrade.Path == __instance.path && upgrade.Tier == tier);

                var text = tierObject.GetComponentInChildren<ModHelperText>(true);

                if (text is null)
                {
                    text = tierObject.gameObject.AddModHelperComponent(
                        ModHelperText.Create(new Info("Queue", InfoPreset.FillParent), ""));
                    text.EnableAutoSizing();
                }

                text.SetText(index > -1 ? (index + 1).ToString() : "");

                var button = tierObject.gameObject.GetComponentOrAdd<Button>();
                if (index > -1)
                {
                    button.onClick.SetListener(() => DequeueUpgrade(QueuedUpgrades[index]));
                }
                else
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Tower), nameof(Tower.OnDestroy))]
    internal static class Tower_OnDestroy
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            TaskScheduler.ScheduleTask(OnQueueChanged, ScheduleType.WaitForFrames, 1);
        }
    }
}