using System;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace UsefulUtilities.Utilities;

public class SandboxClearProjectiles : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Shows a button to clear all projectiles in the Sandbox menu.";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Sandbox;

    [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.Initialise))]
    internal static class BloonMenu_PostInitialised
    {
        [HarmonyPrefix]
        private static void Prefix(BloonMenu __instance)
        {
            if (!GetInstance<SandboxClearProjectiles>().Enabled) return;

            var clearProjectiles = Object.Instantiate(__instance.btnResetDamage, __instance.transform);

            clearProjectiles.image.SetSprite(GetSpriteReference<UsefulUtilitiesMod>(nameof(SandboxClearProjectiles)));

            var matchLocalPosition = clearProjectiles.gameObject.AddComponent<MatchLocalPosition>();
            matchLocalPosition.transformToCopy = __instance.btnResetDamage.transform;
            matchLocalPosition.offset = new Vector3(0, 213, 0);

            clearProjectiles.onClick.SetListener(() => InGame.Bridge.DestroyAllProjectiles());

            clearProjectiles.gameObject.SetActive(true);
        }
    }
}