using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.DailyChallenge;
using Il2CppAssets.Scripts.Unity.UI_New.GameOver;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using Il2CppAssets.Scripts.Unity.UI_New.Legends;
using Il2CppAssets.Scripts.Unity.UI_New.Main.PowersSelect;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class RogueHotkeys : UsefulUtility
{
    public enum SelectMode
    {
        SelectOnChoose,
        SelectOnChooseTwice,
        SelectManually
    }

    protected override bool CreateCategory => true;

    protected override string Icon => VanillaSprites.QuestIconRogueLegend;

    public static readonly ModSettingHotkey Continue = new(KeyCode.Space)
    {
        description = "Catch all hotkey for skipping through menus and selecting the highlighted / default option"
    };

    public static readonly ModSettingHotkey ReRollReward = new(KeyCode.Tab);

    public static readonly ModSettingEnum<SelectMode> SelectionMode = new(SelectMode.SelectOnChooseTwice)
    {
        description = """
                      Select On Choose - Pressing a hotkey immediately chooses and selects the reward.
                      Select on Choose Twice - First press chooses, second press selects.
                      Select Manually - Hotkey only chooses, use the separate Continue hotkey to select.
                      """
    };

    public static readonly ModSettingHotkey ChooseReward1 = new(KeyCode.Alpha1);
    public static readonly ModSettingHotkey ChooseReward2 = new(KeyCode.Alpha2);
    public static readonly ModSettingHotkey ChooseReward3 = new(KeyCode.Alpha3);
    public static readonly ModSettingHotkey ChooseReward4 = new(KeyCode.Alpha4);
    public static readonly ModSettingHotkey ChooseReward5 = new(KeyCode.Alpha5);

    private static ModSettingHotkey[] ChooseRewards =>
    [
        ChooseReward1,
        ChooseReward2,
        ChooseReward3,
        ChooseReward4,
        ChooseReward5
    ];

    private static readonly Dictionary<KeyCode, int> ReorderHotkeys = new()
    {
        {KeyCode.LeftArrow, -1},
        {KeyCode.RightArrow, 1},
        {KeyCode.UpArrow, -5},
        {KeyCode.DownArrow, 5}
    };

    private int delay;

    public override void OnUpdate()
    {
        if (PopupScreen.instance != null &&
            PopupScreen.instance.GetFirstActivePopup().Is(out RogueLootPopup rogueLootPopup))
        {
            delay++;
            if (delay > 30)
            {
                if (ReRollReward.JustReleased() && rogueLootPopup.rerollBtn.IsActive())
                {
                    rogueLootPopup.rerollBtn.onClick.Invoke();
                }

                if (ChooseRewards.Any(hotkey => hotkey.JustReleased()))
                {
                    for (var i = 0; i < ChooseRewards.Length; i++)
                    {
                        if (ChooseRewards[i].JustReleased() && i < rogueLootPopup.activeButtons.Count)
                        {
                            var button = rogueLootPopup.activeButtons.Get(i).GetComponent<RogueLootChoiceButton>();

                            var alreadySelected = rogueLootPopup.selectedButton == button.gameObject;
                            button.selectBtn.onClick.Invoke();
                            if (SelectionMode == SelectMode.SelectOnChoose ||
                                (alreadySelected && SelectionMode == SelectMode.SelectOnChooseTwice))
                            {
                                rogueLootPopup.SelectClicked();
                                delay = 0;
                            }

                            break;
                        }
                    }
                }

                if (Continue.JustReleased())
                {
                    rogueLootPopup.SelectClicked();
                    delay = 0;
                }
            }
        }
        else
        {
            delay = 0;
        }


        if (MenuManager.instance != null &&
            MenuManager.instance.GetCurrentMenu().Is(out RogueMapScreen rogueMapScreen) &&
            rogueMapScreen.RogueSaveData != null &&
            rogueMapScreen.partyDisplay != null &&
            rogueMapScreen.partyDisplay.isActiveAndEnabled &&
            rogueMapScreen.partyDisplay.selectedInsta != null &&
            rogueMapScreen.partyDisplay.selectedInsta.isActiveAndEnabled &&
            ReorderHotkeys.Keys.Any(Input.GetKeyUp))
        {
            var index = rogueMapScreen.partyDisplay.activeInstaIcons.FindIndex(display =>
                display == rogueMapScreen.partyDisplay.selectedInsta);
            var inventory = rogueMapScreen.RogueSaveData.instasInventory;
            var count = inventory.Count;
            var insta = inventory.Get(index);
            inventory.RemoveAt(index);

            foreach (var (key, delta) in ReorderHotkeys)
            {
                if (Input.GetKeyUp(key)) index += delta;
            }
            index = (index + count) % count;

            inventory.Insert(index, insta);

            rogueMapScreen.partyDisplay.Refresh(true);

            var newButton = rogueMapScreen.partyDisplay.activeInstaIcons.Get(index);
            newButton.SetSelected(true);
            rogueMapScreen.partyDisplay.selectedInsta = newButton;
        }


        if (Continue.JustPressed())
        {
            if (PopupScreen.instance != null &&
                PopupScreen.instance.GetFirstActivePopup().Is(out RogueRewardPopup rogueRewardPopup))
            {
                rogueRewardPopup.close.onClick.Invoke();
            }

            if (MenuManager.instance != null &&
                MenuManager.instance.GetCurrentMenu().Is(out RogueVictoryScreen rogueVictoryScreen))
            {
                rogueVictoryScreen.HomeClicked();
            }
        }
    }


    [HarmonyPatch(typeof(RogueRewardPopup), nameof(RogueRewardPopup.Awake))]
    internal static class RogueRewardPopup_Awake
    {
        [HarmonyPrefix]
        internal static void Prefix(RogueRewardPopup __instance)
        {
            if (InGame.instance != null && TimeManager.FastForwardActive)
            {
                __instance.closeDelay /= (float)
                    Math.Max(TimeHelper.OverrideFastForwardTimeScale / Constants.fastForwardTimeScaleMultiplier, 1);
            }
        }
    }

    private static readonly List<Hotkeys.TowerHotkeyInfo> TowerHotkeys = [];

    [HarmonyPatch(typeof(Hotkeys), nameof(Hotkeys.Setup))]
    internal static class Hotkeys_Setup
    {
        [HarmonyPostfix]
        internal static void Postfix(Hotkeys __instance)
        {
            TowerHotkeys.Clear();
            TowerHotkeys.AddRange(__instance.towerHotkeys.ToArray());
        }
    }

    [HarmonyPatch(typeof(InGame), nameof(InGame.CheckShortcutKeys))]
    internal static class InGame_CheckShortcutKeys
    {
        [HarmonyPrefix]
        internal static void Prefix(InGame __instance)
        {
            if (ShopMenu.instance == null || __instance.InputManager.isPlacingTower) return;

            foreach (var towerHotkey in TowerHotkeys)
            {
                if (!__instance.hotkeys.IsHotkeyPressed(towerHotkey.hotkeyButton)) continue;

                var buttons = ShopMenu.instance.ActiveTowerButtons
                    .Where(button => button.TowerModel.baseId == towerHotkey.towerBaseId).ToList();

                if (buttons.Count <= 1) continue;

                towerHotkey.hotkeyButton._isPressed_k__BackingField = false;
                towerHotkey.hotkeyButton.isPressed = false;
                towerHotkey.hotkeyButton._wasPressedThisFrame_k__BackingField = false;
                towerHotkey.hotkeyButton.wasPressedThisFrame = false;

                var index = (buttons.FindIndex(button => ShopMenu.instance.selectedButton == button) + 1) %
                            buttons.Count;

                for (var i = 1; i <= buttons.Count; i++)
                {
                    var tryIndex = (buttons.Count + index - i) % buttons.Count;

                    if (buttons[tryIndex].IsValidReference())
                    {
                        buttons[tryIndex].ButtonActivated();
                    }
                }
            }
        }
    }
}