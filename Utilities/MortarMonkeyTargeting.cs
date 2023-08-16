using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;

namespace UsefulUtilities.Utilities;

public class MortarMonkeyTargeting : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Gives all Mortar Monkeys First/Last/Close/Strong targeting";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Targeting;

    protected override string Icon => VanillaSprites.MortarMonkeyIcon;

    protected override string DisableIfModPresent => "MegaKnowledge";

    public override void OnNewGameModel(GameModel gameModel)
    {
        if (!Enabled) return;

        foreach (var model in gameModel.GetTowersWithBaseId(TowerType.MortarMonkey))
        {
            var attackModel = model.GetAttackModel();


            var targetSelectedPointModel = attackModel.GetBehavior<TargetSelectedPointModel>();
            attackModel.RemoveBehavior<TargetSelectedPointModel>();
            attackModel.targetProvider = null;

            attackModel.AddBehavior(new TargetFirstModel("", true, false));
            attackModel.AddBehavior(new TargetLastModel("", true, false));
            attackModel.AddBehavior(new TargetCloseModel("", true, false));
            attackModel.AddBehavior(new TargetStrongModel("", true, false));
            
            attackModel.AddBehavior(targetSelectedPointModel);
            
            model.towerSelectionMenuThemeId = "ActionButton";
            model.UpdateTargetProviders();
        }
    }
}