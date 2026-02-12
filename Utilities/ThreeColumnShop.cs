using System;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.UI;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppNinjaKiwi.Common;
using UnityEngine;
using UnityEngine.UI;
namespace UsefulUtilities.Utilities;

public class ThreeColumnShop : ToggleableUtility
{
    protected override bool DefaultEnabled => false;

    public override string Description => "Makes the in game list of towers in the shop have 3 columns instead of 2.";

    protected override string Icon => VanillaSprites.Coop3PlayersIcon;

    private static bool lastEnabled;

    public override void OnRegister()
    {
        base.OnRegister();

        Messaging<OnScreenSizeDidChange>.Register(new Action<int, int, bool>((_, _, _) =>
        {
            if (ShopMenu.instance != null)
            {
                UpdateShop(ShopMenu.instance);
            }
        }));
    }

    private void UpdateShop(ShopMenu shopMenu)
    {
        var gridLayoutGroup = shopMenu.towerButtons.GetComponent<GridLayoutGroup>();

        if (gridLayoutGroup.constraintCount == 1) return;

        var rectTransform = shopMenu.towerButtons.transform.Cast<RectTransform>();
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.localScale = Vector3.one * (Enabled ? 2f / 3f : 1);

        gridLayoutGroup.constraintCount = Enabled ? 3 : 2;
    }

    [HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.Initialise))]
    internal static class ShopMenu_Initialise
    {
        [HarmonyPrefix]
        internal static void Prefix()
        {
            lastEnabled = false;
        }
    }

    [HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.Update))]
    internal static class ShopMenu_OnUpdate
    {
        [HarmonyPostfix]
        internal static void Postfix(ShopMenu __instance)
        {
            var threeColumnShop = GetInstance<ThreeColumnShop>();

            if (threeColumnShop.Enabled != lastEnabled)
            {
                threeColumnShop.UpdateShop(__instance);
            }

            lastEnabled = threeColumnShop.Enabled;
        }
    }
}