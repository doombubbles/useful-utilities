using System.Linq;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using Il2CppTMPro;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class HotkeyDisplay : ToggleableUtility
{
    private static readonly ModSettingBool ShortenModifiers = new(false)
    {
        description = "Shortens modifier keys.\n" +
                      "Control becomes ^\n" +
                      "Shift becomes +\n" +
                      "Alt becomes !"
    };

    private static readonly ModSettingDouble TextOpacity = new(1)
    {
        description = "Change how transparent the hotkey text is. 1 is fully solid, 0 is invisible.",
        min = 0,
        max = 1,
        slider = true,
        stepSize = .01f
    };

    protected override bool CreateCategory => true;

    protected override bool DefaultEnabled => true;

    public override string Description => "Shows the hotkeys used to place towers within the Tower Shop Menu";

    protected override string Icon => VanillaSprites.HotkeysIcon;

    private static void UpdateHotkeyDisplay(TowerPurchaseButton button, HotkeyButton hotkeyButton)
    {
        if (button == null) return;

        var hotkey = hotkeyButton.hotkey;
        var gameObject = button.gameObject;

        var text = gameObject.GetComponentInChildren<ModHelperText>(true);

        if (text == null)
        {
            text = gameObject.AddModHelperComponent(ModHelperText.Create(new Info("Hotkey")
            {
                Width = -50, Height = 75, AnchorMin = new Vector2(0, 1), AnchorMax = new Vector2(1, 1),
                Pivot = new Vector2(0.5f, 1)
            }, "", 48, TextAlignmentOptions.Right));
            text.transform.MoveAfterSibling(button.costText.transform, true);
            text.Text.fontSizeMax = 48;
            text.Text.enableAutoSizing = true;
            text.Text.color = new Color(1, 1, 1, TextOpacity);
        }

        if (string.IsNullOrWhiteSpace(hotkey.path) || hotkey.path.Contains("None"))
        {
            text.SetActive(false);
            return;
        }

        var key = hotkey.path.Split('/').Last();

        text.SetActive(true);

        var modifier = hotkey.modifierKey == HotkeyModifier.None
            ? ""
            : ShortenModifiers
                ? hotkey.modifierKey switch
                {
                    HotkeyModifier.Ctrl => "^",
                    HotkeyModifier.Shift => "+",
                    HotkeyModifier.Alt => "!",
                    _ => ""
                }
                : hotkey.modifierKey + "+";

        text.SetText(modifier + key.ToUpper());
    }

    [HarmonyPatch(typeof(Hotkeys), nameof(Hotkeys.Setup))]
    internal static class Hotkeys_Setup
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Hotkeys __instance)
        {
            if (!GetInstance<HotkeyDisplay>().Enabled) return;

            if (__instance.HeroButton.Is(out TowerPurchaseButton towerPurchaseButton))
            {
                UpdateHotkeyDisplay(towerPurchaseButton, __instance.heroHotkey);
            }

            foreach (var towerHotkey in __instance.towerHotkeys)
            {
                if (towerHotkey.towerPurchaseButton.Is(out towerPurchaseButton))
                {
                    UpdateHotkeyDisplay(towerPurchaseButton, towerHotkey.hotkeyButton);
                }
            }
        }
    }
}