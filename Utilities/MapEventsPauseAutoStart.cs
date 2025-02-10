using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation;
namespace UsefulUtilities.Utilities;

public class MapEventsPauseAutoStart : UsefulUtility
{
    protected override bool CreateCategory => true;
    public override string Description =>
        "Pauses auto start for the rounds immediately after certain significant map events.";

    public static readonly ModSettingBool Polyphemus = new(true)
    {
        description = "Pause auto start when the Eye closes.",
        icon = VanillaSprites.MapSelectPolyphemusMapButton
    };

    public static readonly ModSettingBool Erosion = new(true)
    {
        description = "Pause auto start when the ice erodes.",
        icon = VanillaSprites.MapSelectErosionButton
    };

    private static readonly int[] ErosionRounds = [17, 35, 53, 71, 89];

    [HarmonyPatch(typeof(Simulation), nameof(Simulation.RoundEnd))]
    internal static class Simulation_RoundEnd
    {
        [HarmonyPrefix]
        internal static void Prefix(Simulation __instance, int round)
        {
            var nextRound = round + 1;
            if (__instance.Map.mapModel.mapName == "Polyphemus" && Polyphemus && nextRound % 10 == 5 ||
                __instance.Map.mapModel.mapName == "Erosion" && Erosion && ErosionRounds.Contains(nextRound))
            {
                __instance.pauseAutoPlay = true;
            }
        }
    }

}