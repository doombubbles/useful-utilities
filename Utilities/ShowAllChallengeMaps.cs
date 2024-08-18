using System.Reflection;
using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Unity.UI_New.ChallengeEditor;
using Il2CppAssets.Scripts.Unity.UI_New.DailyChallenge;

namespace UsefulUtilities.Utilities;

public class ShowAllChallengeMaps : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Shows hidden maps like Blons in the Challenge Editor without any restrictions.";

    [HarmonyPatch]
    internal static class ChallengeEditor_MapSelectClicked
    {
        private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ChallengeEditor), nameof(ChallengeEditor.MapSelectClicked));
            yield return AccessTools.Method(typeof(BossEventScreen), nameof(BossEventScreen.MapSelectClicked));
        }
        
        [HarmonyPrefix]
        private static void Prefix(ref bool[] __state)
        {
            if (!GetInstance<ShowAllChallengeMaps>().Enabled) return;

            var maps = GameData.Instance.mapSet.Maps.items;
            __state = new bool[maps.Length];

            for (var i = 0; i < maps.Length; i++)
            {
                __state[i] = maps[i].isBrowserOnly;
                if (maps[i].id != "BaseEditorMap")
                {
                    maps[i].isBrowserOnly = false;
                }
            }
        }

        [HarmonyPostfix]
        private static void Postfix(ref bool[] __state)
        {
            if (!GetInstance<ShowAllChallengeMaps>().Enabled) return;

            var maps = GameData.Instance.mapSet.Maps.items;

            for (var i = 0; i < maps.Length; i++)
            {
                maps[i].isBrowserOnly = __state[i];
            }
        }
    }
    
    
}