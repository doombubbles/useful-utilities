using BTD_Mod_Helper.Api.Enums;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace UsefulUtilities.Utilities;

public class QuickSell : ToggleableUtility
{
    protected override bool DefaultEnabled => true;
    public override string Description => "If you hold down the Sell Tower hotkey, you will sell towers as you select them.";
    protected override string Icon => VanillaSprites.MoneyBag;

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.SelectTower))]
    internal static class TowerSelectionMenu_SelectTower
    {
        [HarmonyPostfix]
        internal static bool Prefix(TowerToSimulation tower)
        {
            if (!InGame.instance || InGame.Bridge == null || InGame.instance.ReviewMapMode || InGame.Bridge.IsSpectatorMode) return true;
            
            if (!InGame.instance.hotkeys.sell.isPressed || !GetInstance<QuickSell>().Enabled) return true;
            
            InGame.instance.SellTower(tower);
            return false;
        }
    }
}