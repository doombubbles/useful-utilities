using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu;
using UnityEngine;
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

            var clearProjectiles = Object.Instantiate(__instance.btnResetDamage,
                __instance.btnResetDamage.transform.parent, false);

            clearProjectiles.transform.SetAsFirstSibling();
            clearProjectiles.image.SetSprite(GetSpriteReference<UsefulUtilitiesMod>(nameof(SandboxClearProjectiles)));
            clearProjectiles.onClick.SetListener(() => InGame.Bridge.DestroyAllProjectiles());
            clearProjectiles.gameObject.SetActive(true);
            clearProjectiles.transform.parent.parent.GetComponent<RectTransform>().sizeDelta += new Vector2(0, 420);
            clearProjectiles.transform.parent.localPosition += new Vector3(0, 420 * .75f, 0);
        }
    }
}