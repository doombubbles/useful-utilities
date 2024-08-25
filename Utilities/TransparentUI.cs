using System;
using System.Collections.Generic;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2Cpp;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.Utils;
using Il2CppNinjaKiwi.Common;
using Il2CppNinjaKiwi.Common.ResourceUtils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UsefulUtilities.Utilities;

public class TransparentUI : UsefulUtility
{
    public static readonly ModSettingDouble InGameUITransparency = new(1)
    {
        displayName = "In Game UI Transparency",
        description = "Modifies the transparency level of the In Game UI elements like the tower selection menu. " +
                      "Setting the transparency all the way to 0 will also make the elements non interactable (since they're invisible).",
        slider = true,
        min = 0,
        max = 1,
        stepSize = .01f,
        icon = VanillaSprites.GhostBloonIcon
    };

    private static IEnumerable<GameObject?> InGameUIElements()
    {
        yield return InGame.instance?.mapRect?.gameObject;
        yield return TowerSelectionMenu.instance?.gameObject;
        yield return MainHudLeftAlign.instance?.gameObject;
        yield return MainHudRightAlign.instance?.gameObject;
    }
    
    public override void OnUpdate()
    {
        if (InGame.instance != null)
        {
            foreach (var element in InGameUIElements())
            {
                if (element == null) continue;
                
                var canvasGroup = element.GetComponentOrAdd<CanvasGroup>();
                canvasGroup.alpha = InGameUITransparency;
                canvasGroup.interactable = InGameUITransparency > 0;
                
                canvasGroup.blocksRaycasts = InGameUITransparency > 0 || element.HasComponent<InGameMapRect>();
            }
        }

        if (MenuManager.instance == null ||
            !MenuManager.instance.GetCurrentMenu().Is(out AccessibilitySettingsUI __instance) ||
            __instance.transform.FindChild("NewPanel") != null)
            return;

        var newPanel = __instance.gameObject.AddModHelperScrollPanel(
            new Info("NewPanel", 2100, 2000),
            RectTransform.Axis.Vertical,
            VanillaSprites.MainBgPanel,
            50,
            50
        );

        try
        {
            var panel = __instance.transform.Find("Panel");

            AddSlider(__instance, newPanel);

            AddSlider(__instance, newPanel, info =>
            {
                info.Title.AutoLocalize = false;
                info.Title.SetText(InGameUITransparency.displayName);
                info.Description.AutoLocalize = false;
                info.Description.SetText(InGameUITransparency.description +
                                         " Changing this setting here is temporary, go to Mod Settings to make the change persistent.");

                info.Icon.LoadSprite(new SpriteReference(InGameUITransparency.icon));
                info.Icon.gameObject.RemoveComponent<Button>();

                info.Slider.gameObject.RemoveComponent<EffectScaleControl>();
                info.Slider.onValueChanged.RemoveAllListeners();
                info.Slider.onValueChanged.AddListener(new Action<float>(percent =>
                {
                    var value = percent / 100;
                    info.Percent.SetText(value > 0 ? $"{value:P0}" : "Off");
                    InGameUITransparency.SetValue((double) value);
                }));

                TaskScheduler.ScheduleTask(() => info.Slider.Set(InGameUITransparency * 100));
            });

            var okBtn = __instance.gameObject.GetComponentInChildrenByName<RectTransform>("OkBtn");
            var newOkBtn = Object.Instantiate(okBtn, __instance.transform);
            newOkBtn.Translate(0, 60, 0);

            panel.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            ModHelper.Error<UsefulUtilitiesMod>(e);
            newPanel.SetActive(false);
        }
    }

    public record SliderInfo(
        NK_TextMeshProUGUI Title,
        Slider Slider,
        Image Icon,
        NK_TextMeshProUGUI Percent,
        NK_TextMeshProUGUI Description);

    public void AddSlider(AccessibilitySettingsUI __instance, ModHelperScrollPanel newPanel,
        Action<SliderInfo>? modify = null)
    {
        var oldTitle = __instance.gameObject.GetComponentInChildrenByName<RectTransform>("Title");
        var fxSlider = __instance.gameObject.GetComponentInChildrenByName<RectTransform>("FxSlider");
        var fxDescrition = __instance.gameObject.GetComponentInChildrenByName<RectTransform>("FxDescription");

        var newTitle = Object.Instantiate(oldTitle, newPanel.ScrollContent);
        var newTitleLayout = newTitle.gameObject.AddComponent<LayoutElement>();
        newTitleLayout.preferredWidth = oldTitle.rect.width;
        newTitleLayout.preferredHeight = oldTitle.rect.height;

        var newSlider = Object.Instantiate(fxSlider, newPanel.ScrollContent);
        var newSliderLayout = newSlider.gameObject.AddComponent<LayoutElement>();
        newSliderLayout.preferredWidth = fxSlider.rect.width;
        newSliderLayout.preferredHeight = fxSlider.rect.height;

        var newDescrition = Object.Instantiate(fxDescrition, newPanel.ScrollContent);
        var newDescritionLayout = newDescrition.gameObject.AddComponent<LayoutElement>();
        newDescritionLayout.preferredWidth = fxDescrition.rect.width;
        newDescritionLayout.preferredHeight = fxDescrition.rect.height;

        modify?.Invoke(new SliderInfo(
            newTitle.GetComponentInChildren<NK_TextMeshProUGUI>(),
            newSlider.GetComponentInChildren<Slider>(),
            newSlider.gameObject.GetComponentInChildrenByName<Image>("Icon"),
            newSlider.gameObject.GetComponentInChildrenByName<NK_TextMeshProUGUI>("PercentText"),
            newDescritionLayout.GetComponentInChildren<NK_TextMeshProUGUI>()
        ));
    }
}