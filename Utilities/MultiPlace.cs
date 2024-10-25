using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class MultiPlace : UsefulUtility
{
    public static readonly ModSettingHotkey MultiPlaceModifier = new(KeyCode.LeftShift)
    {
        description = "Universal key for holding and placing multiple towers / powers / instas / items."
    };

    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExitTowerMode))]
    internal static class InputManager_ExitTowerMode
    {
        [HarmonyPrefix]
        internal static void Prefix(InputManager __instance)
        {
            if (!MultiPlaceModifier.IsPressed() || !__instance.inTowerMode) return;
            
            var tb = __instance.towerButton;
            var tm = __instance.towerModel;
            TaskScheduler.ScheduleTask(() => __instance.PrimeTower(tb, tm));
        }
    }
    
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExitPowerMode))]
    internal static class InputManager_ExitPowerMode
    {
        [HarmonyPrefix]
        internal static void Prefix(InputManager __instance)
        {
            if (!MultiPlaceModifier.IsPressed() || !__instance.inPowerMode) return;
            
            var pb = __instance.powerButton;
            var pm = __instance.powerModel;
            TaskScheduler.ScheduleTask(() => __instance.PrimePower(pb, pm));
        }
    }
    
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExitInstaMode))]
    internal static class InputManager_ExitInstaMode
    {
        [HarmonyPrefix]
        internal static void Prefix(InputManager __instance)
        {
            if (!MultiPlaceModifier.IsPressed() || !__instance.inInstaMode) return;
            
            var ib = __instance.instaButton;
            var im = __instance.instaModel;
            TaskScheduler.ScheduleTask(() => __instance.PrimeInsta(ib, im));
        }
    }
    
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ExitGeraldoItemMode))]
    internal static class InputManager_ExitGeraldoItemMode
    {
        [HarmonyPrefix]
        internal static void Prefix(InputManager __instance)
        {
            if (!MultiPlaceModifier.IsPressed() || !__instance.inGeraldoShopItemMode) return;
            
            var itemUi = __instance.towerBasedShopItemUi;
            var model = __instance.towerShopItemModel;
            TaskScheduler.ScheduleTask(() => __instance.PrimeTowerShopItem(itemUi, model));
        }
    }
}