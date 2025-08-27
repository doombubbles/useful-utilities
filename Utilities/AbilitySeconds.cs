using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.AbilitiesMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppTMPro;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace UsefulUtilities.Utilities;

public class AbilitySeconds : UsefulUtility
{
    protected override bool CreateCategory => true;

    public override string Description => "Makes Ability buttons display their cooldown in seconds";

    public static readonly ModSettingDouble AbilityTextOpacity = new(1)
    {
        icon = VanillaSprites.EmoteTextSpeechBubble,
        description = "Ability Timer Text Opacity. 0.0 = text disabled",
        min = 0,
        max = 1,
        slider = true,
        stepSize = 0.01f,
    };

    public static readonly ModSettingInt DecimalPlaces = new(1)
    {
        icon = VanillaSprites.EnterCodeIcon,
        min = 0,
        max = 2,
        description = "Amount of decimal places to display.",
        slider = true,
    };

    public static readonly ModSettingBool EnableTextColor = new(true)
    {
        icon = VanillaSprites.Rainbow2,
        description = "Enable a red hue for abilities. The more time on a cooldown, the deeper the red.",
    };

    public static readonly ModSettingBool EnableDefaultCooldownCircle = new(false)
    {
        icon = VanillaSprites.CooldownClockBg,
        description = "Enable the default cooldown circle.",
    };

    public static readonly ModSettingBool EnableTrailingS = new(true)
    {
        description = "Enable to have \"s\" appended to timers (i.e. 6.9s opposed to 6.9).",
    };


    [HarmonyPatch(typeof(AbilityMenu), nameof(AbilityMenu.Update))]
    internal static class AbilityMenu_Update
    {
        [HarmonyPrefix]
        internal static void Prefix(AbilityMenu __instance)
        {
            foreach (var a in __instance.GetAbilitiesButtons())
            {
                var fastestAbility = a.abilities.ToArray()
                    .OrderBy(x => x.CooldownRemaining)
                    .First();

                ApplyCooldownText(fastestAbility, a);
            }
        }
    }

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.OnUpdate))]
    internal static class TowerSelectionMenu_OnUpdate
    {
        [HarmonyPrefix]
        internal static void Prefix(TowerSelectionMenu __instance)
        {
            foreach (var a in __instance.abilityButtons)
            {
                ApplyCooldownText(a.ability, a);
            }
        }
    }

    private static void ApplyCooldownText(AbilityToSimulation? ability, AbilityButton abilityButton)
    {
        if (ability is null) return;
        // Check if we've already added our panel
        var existing = abilityButton.transform.Find("AbilitySecondsTextPanel");

        abilityButton.cooldownFade?.gameObject.SetActive(EnableDefaultCooldownCircle || AbilityTextOpacity == 0);

        if (ability.IsReady || ability.CurrentAdditionalCharges > 0)
        {
            if (existing) existing.gameObject.Destroy();
            return;
        }

        var cooldown = ability.CooldownRemaining;

        ModHelperPanel textPanel;

        if (existing == null)
        {
            var rectTransform = abilityButton.GetComponent<RectTransform>();
            // Make the panel
            textPanel = abilityButton.gameObject.AddModHelperPanel(
                new Info("AbilitySecondsTextPanel", 0, 0, rectTransform.rect.width, rectTransform.rect.height));

            // Add the text element
            var newText = textPanel.AddText(
                new Info("AbilitySecondsText", 0, 0, rectTransform.rect.width + 25, rectTransform.rect.height - 30),
                text: "",
                fontSize: 68f,
                align: TextAlignmentOptions.Bottom);
            newText.Text.fontStyle |= FontStyles.SmallCaps;

            newText.transform.SetParent(textPanel.transform);
        }
        else
        {
            textPanel = existing.GetComponent<ModHelperPanel>();
        }

        // Update the text only
        var text = textPanel.transform.Find("AbilitySecondsText").GetComponent<ModHelperText>();

        if (EnableTextColor)
        {
            var t = ability.CooldownRemaining / ability.CooldownTotal;
            text.Text.color = new Color(1f, 1f - t, 1f - t, AbilityTextOpacity);
        }
        else
        {
            text.Text.color = new Color(1f, 1f, 1f, AbilityTextOpacity);
        }

        var s = $"{string.Format("{0:F" + DecimalPlaces.GetValue() + "}", cooldown)}{(EnableTrailingS ? "s" : "")}";
        text.SetText(s);
    }
}