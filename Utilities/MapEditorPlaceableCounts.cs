using System.Collections.Generic;
using System.Reflection;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Map;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.EditorMenus;

namespace UsefulUtilities.Utilities;

public class MapEditorPlaceableCounts : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Shows the Map Editor placeable objects (props/stamps) current counts and maximum in the category title";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Sandbox;

    private static void UpdateTitle(EditorMenuPopout editorMenu)
    {
        if (!GetInstance<MapEditorPlaceableCounts>().Enabled) return;

        if (editorMenu.selectedCategory is CategoryButton.PropsPanel)
        {
            editorMenu.categoryTitleText.SetText(
                $"{"Props".Localize()} ({InGame.Bridge.GetAllProps().Count()}/{editorMenu.EditorSettings.MaxPlaceables})");
        }

        if (editorMenu.selectedCategory is CategoryButton.StampsPanel)
        {
            editorMenu.categoryTitleText.SetText(
                $"{"Stamps".Localize()} ({editorMenu.MapEditorSceneController.GetStampCount()}/{editorMenu.EditorSettings.MaxPlaceables})");
        }
    }

    [HarmonyPatch]
    internal static class EditorMenuPopout_Update
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(EditorMenuPopout),
                nameof(EditorMenuPopout.OnCategoryButtonClicked));
            yield return AccessTools.Method(typeof(EditorMenuPopout),
                nameof(EditorMenuPopout.OnPlaceablesPlacedOrRemoved));
            yield return AccessTools.Method(typeof(EditorMenuPopout),
                nameof(EditorMenuPopout.OnStampErasedEventTriggered));
        }

        [HarmonyPostfix]
        internal static void Postfix(EditorMenuPopout __instance)
        {
            UpdateTitle(__instance);
        }
    }
}