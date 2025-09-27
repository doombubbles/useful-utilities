using HarmonyLib;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppSystem.Collections.Generic;

namespace UsefulUtilities.Utilities;

public class FixBananaFarmerPro : UsefulUtility
{
    [HarmonyPatch(typeof(BananaFarmerRegrowBananas), nameof(BananaFarmerRegrowBananas.LateSetSaveMetaData))]
    internal static class BananaFarmerRegrowBananas_LateSetSaveMetaData
    {
        [HarmonyPrefix]
        internal static bool Prefix(BananaFarmerRegrowBananas __instance, Dictionary<string, string> metaData)
        {
            if (metaData.TryGetValue("Tech Bot Link Id", out var id))
            {
                var towerId = id.DeserializeObjectId();
                var tower = __instance.Sim.towerManager.GetTowerById(towerId);
                __instance.linkedTower = tower;
            }

            return false;
        }
    }
}