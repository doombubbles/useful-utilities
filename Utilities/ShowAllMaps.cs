using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Unity.UI_New.ChallengeEditor;

namespace UsefulUtilities.Utilities;

public class ShowAllMaps : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Shows hidden maps like Blons in the Challenge Editor without any restrictions.";

    [HarmonyPatch(typeof(ChallengeEditor), nameof(ChallengeEditor.MapSelectClicked))]
    internal static class ChallengeEditor_MapSelectClicked
    {
        [HarmonyPrefix]
        private static void Prefix(ref bool[] __state)
        {
            if (!GetInstance<ShowAllMaps>().Enabled) return;

            var maps = GameData.Instance.mapSet.maps;
            __state = new bool[maps.Length];

            for (var i = 0; i < maps.Length; i++)
            {
                __state[i] = maps[i].isBrowserOnly;
                maps[i].isBrowserOnly = false;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(ref bool[] __state)
        {
            if (!GetInstance<ShowAllMaps>().Enabled) return;

            var maps = GameData.Instance.mapSet.maps;

            for (var i = 0; i < maps.Length; i++)
            {
                maps[i].isBrowserOnly = __state[i];
            }
        }
    }
}