using BTD_Mod_Helper.Api.Enums;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MapSelect;

namespace UsefulUtilities.Utilities;

public class HideFriendScores : ToggleableUtility
{
    protected override bool DefaultEnabled => false;

    public override string Description =>
        "Hides the indicators on the Map Select Screen of the highest round completed amongst you and your friends.";

    protected override string Icon => VanillaSprites.FriendsIcon;

    [HarmonyPatch(typeof(MapButton), nameof(MapButton.Init))]
    internal static class MapButton_Init
    {
        [HarmonyPostfix]
        internal static void Postfix(MapButton __instance)
        {
            if (GetInstance<HideFriendScores>().Enabled)
            {
                __instance.friendPanel.gameObject.SetActiveRecursively(false);
            }
        }
    }
}