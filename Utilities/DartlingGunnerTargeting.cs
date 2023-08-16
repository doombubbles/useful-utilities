using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;

namespace UsefulUtilities.Utilities;

public class DartlingGunnerTargeting : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Gives all Dartling Gunners First/Last/Close/Strong targeting";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Targeting;

    protected override string Icon => VanillaSprites.DartlingGunnerIcon;

    protected override string DisableIfModPresent => "MegaKnowledge";

    public override void OnNewGameModel(GameModel gameModel)
    {
        if (!Enabled) return;

        foreach (var model in gameModel.GetTowersWithBaseId(TowerType.DartlingGunner))
        {
            if (model.appliedUpgrades.Contains(UpgradeType.BloonAreaDenialSystem)) return;

            var attackModel = model.GetAttackModel();

            attackModel.AddBehavior(new RotateToTargetModel("", false, false, false, 0,
                false, false));


            var targetPointerModel = attackModel.GetBehavior<TargetPointerModel>();
            var targetSelectedPointModel = attackModel.GetBehavior<TargetSelectedPointModel>();

            attackModel.RemoveBehavior<TargetPointerModel>();
            attackModel.RemoveBehavior<TargetSelectedPointModel>();

            attackModel.AddBehavior(new TargetFirstModel("", true, false));
            attackModel.AddBehavior(new TargetLastModel("", true, false));
            attackModel.AddBehavior(new TargetCloseModel("", true, false));
            attackModel.AddBehavior(new TargetStrongModel("", true, false));

            attackModel.AddBehavior(targetPointerModel);
            attackModel.AddBehavior(targetSelectedPointModel);

            if (attackModel.HasDescendant(out LineEffectModel lineEffectModel))
            {
                lineEffectModel.useRotateToPointer = false;
            }

            model.UpdateTargetProviders();
        }
    }
}