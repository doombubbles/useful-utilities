using System.Collections.Generic;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.UI_New;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.Utils;
using Il2CppNinjaKiwi.Common;
using UnityEngine;

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
    }
}