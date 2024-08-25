using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.ActionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if USEFUL_UTILITIES
namespace UsefulUtilities.Utilities;
#else
using BTD_Mod_Helper.Api;

namespace BetterAutoStart;
#endif

#if USEFUL_UTILITIES
public class BetterAutoStart : ToggleableUtility
#else
public class BetterAutoStartUtility
#endif
{
#if USEFUL_UTILITIES
    protected override bool DefaultEnabled => true;

    public override string Description => "Makes the Auto Start setting visible on the Play button, " +
                                          "and causes right clicking the play button to toggle Auto Start";

    private static bool IsEnabled => GetInstance<BetterAutoStart>().Enabled;

    private static Sprite GetSprite(string name) => GetSprite<UsefulUtilitiesMod>(name);
#else
    private const bool IsEnabled = true;

    private static Sprite GetSprite(string name) => ModContent.GetSprite<BetterAutoStartMod>(name);
#endif

    private static void UpdateTextures(GoFastForwardToggle toggle, bool enabled)
    {
        if (enabled)
        {
            toggle.goImage.GetComponent<Image>()
                .SetSprite(GetSprite("GoBtn")!);
            toggle.fastForwardOffImage.GetComponent<Image>()
                .SetSprite(GetSprite("FastForwardBtn")!);
            toggle.fastForwardOnImage.GetComponent<Image>()
                .SetSprite(GetSprite("FastForwardGlowBtn")!);
        }
        else
        {
            toggle.goImage.GetComponent<Image>().SetSprite(VanillaSprites.GoBtn);
            toggle.fastForwardOffImage.GetComponent<Image>().SetSprite(VanillaSprites.FastForwardBtn);
            toggle.fastForwardOnImage.GetComponent<Image>().SetSprite(VanillaSprites.FastForwardGlowBtn);
        }
    }

    [HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
    internal class Button_OnPointerClick
    {
        [HarmonyPostfix]
        internal static void Postfix(Button __instance, PointerEventData eventData)
        {
            if (InGame.instance == null ||
                eventData.button != PointerEventData.InputButton.Right ||
                __instance.name != "FastFoward-Go" || // yes this is a real typo in the name
                !IsEnabled) return;

            var newValue = !InGame.instance.bridge.simulation.autoPlay;
            Game.instance.GetPlayerProfile().inGameSettings.autoPlay = newValue;
            InGame.instance.bridge.SetAutoPlay(newValue);
        }
    }

    [HarmonyPatch(typeof(GoFastForwardToggle), nameof(GoFastForwardToggle.OnEnable))]
    internal static class GoFastForwardToggle_OnEnable
    {
        [HarmonyPostfix]
        private static void Postfix(GoFastForwardToggle __instance)
        {
            if (IsEnabled)
            {
                UpdateTextures(__instance, Game.instance.GetPlayerProfile().inGameSettings.autoPlay);
            }
        }
    }

    [HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.SetAutoPlay))]
    internal static class UnityToSimulation_ToggleAutoPlay
    {
        [HarmonyPostfix]
        private static void Postfix(bool on)
        {
            if (ShopMenu.instance != null && ShopMenu.instance.goFFToggle != null && IsEnabled)
            {
                UpdateTextures(ShopMenu.instance.goFFToggle, on);
            }
        }
    }
}