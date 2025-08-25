using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.AbilitiesMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppTMPro;
using MelonLoader;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class AbilitySeconds : UsefulUtility
{
    public static readonly ModSettingDouble TextOpacity = new(1)
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

                ApplyCooldownText(fastestAbility, a.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.OnUpdate))]
    internal static class AbilityButton
    {
        [HarmonyPrefix]
        internal static void Prefix(TowerSelectionMenu __instance)
        {
            foreach (var a in __instance.abilityButtons)
            {
                ApplyCooldownText(a.ability, a.gameObject);
            }
        }
    }

    private static void ApplyCooldownText(AbilityToSimulation ability, GameObject abilityGameObject)
    {
        if (ability is null) return;
        // Check if we've already added our panel
        var existing = abilityGameObject.transform.Find("AbilitySecondsTextPanel");

        var cooldownObj = abilityGameObject.transform.Find("CooldownEffect");
        cooldownObj?.gameObject.SetActive(EnableDefaultCooldownCircle);

        if (ability.IsReady)
        {
            if (existing) existing.gameObject.Destroy();
            return;
        }

        var cooldown = ability.CooldownRemaining;

        ModHelperPanel textPanel;

        if (existing == null)
        {
            var rectTransform = abilityGameObject.GetComponent<RectTransform>();
            // Make the panel
            textPanel = abilityGameObject.AddModHelperPanel(
                new("AbilitySecondsTextPanel", 0, 0, rectTransform.rect.width, rectTransform.rect.height));

            // Add the text element
            var newText = textPanel.AddText(
                new("AbilitySecondsText", 0, 0, rectTransform.rect.width + 25, rectTransform.rect.height - 30),
                text: "",
                fontSize: 68f,
                align: TextAlignmentOptions.Bottom);

            newText.transform.SetParent(textPanel.transform);
        }
        else
        {
            textPanel = existing.GetComponent<ModHelperPanel>();
        }

        // Update the text only
        var text = textPanel.transform.Find("AbilitySecondsText")
            .GetComponent<ModHelperText>();

        if (EnableTextColor)
        {
            var t = ability.CooldownRemaining / ability.CooldownTotal;
            text.Text.color = new Color(1f, 1f - t, 1f - t, TextOpacity);
        }
        else
        {
            text.Text.color = new Color(1f, 1f, 1f, TextOpacity);
        }

        string s = $"{string.Format("{0:F" + DecimalPlaces.GetValue() + "}", cooldown)}{(EnableTrailingS ? "s" : "")}";
        text.SetText(s);
    }
}
