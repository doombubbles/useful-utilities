using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Coop;

namespace UsefulUtilities.Utilities;

public class SinglePlayerCoop : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Allows you to start Co-Op lobbies even if you're the only person there.";

    [HarmonyPatch(typeof(CoopLobbyScreen), nameof(CoopLobbyScreen.Update))]
    internal static class CoopLobbyScreen_Update
    {
        [HarmonyPostfix]
        public static void AllowSinglePlayerCoop(CoopLobbyScreen __instance)
        {
            if (GetInstance<SinglePlayerCoop>().Enabled)
            {
                __instance.readyBtn.enabled = true;
                __instance.readyBtn.interactable = true;
            }
        }
    }
}