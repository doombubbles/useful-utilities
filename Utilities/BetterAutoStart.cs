using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.ActionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities;

public class BetterAutoStart : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Makes the Auto Start setting visible on the Play button, " +
                                          "and causes right clicking the play button to toggle Auto Start";


    private static void UpdateTextures(GoFastForwardToggle toggle, bool enabled)
    {
        if (enabled)
        {
            toggle.goImage.GetComponent<Image>()
                .SetSprite(GetSprite<UsefulUtilitiesMod>("GoBtn")!);
            toggle.fastForwardOffImage.GetComponent<Image>()
                .SetSprite(GetSprite<UsefulUtilitiesMod>("FastForwardBtn")!);
            toggle.fastForwardOnImage.GetComponent<Image>()
                .SetSprite(GetSprite<UsefulUtilitiesMod>("FastForwardGlowBtn")!);
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
            if (InGame.instance != null &&
                eventData.button == PointerEventData.InputButton.Right &&
                __instance.name == "FastFoward-Go" &&
                GetInstance<BetterAutoStart>().Enabled) // yes this is a real typo in the name
            {
                var newValue = !InGame.instance.bridge.simulation.autoPlay;
                Game.instance.GetPlayerProfile().inGameSettings.autoPlay = newValue;
                InGame.instance.bridge.SetAutoPlay(newValue);
            }
        }
    }

    [HarmonyPatch(typeof(GoFastForwardToggle), nameof(GoFastForwardToggle.OnEnable))]
    internal static class GoFastForwardToggle_OnEnable
    {
        [HarmonyPostfix]
        private static void Postfix(GoFastForwardToggle __instance)
        {
            if (GetInstance<BetterAutoStart>().Enabled)
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
            if (ShopMenu.instance != null &&
                ShopMenu.instance.goFFToggle != null &&
                GetInstance<BetterAutoStart>().Enabled)
            {
                UpdateTextures(ShopMenu.instance.goFFToggle, on);
            }
        }
    }
}