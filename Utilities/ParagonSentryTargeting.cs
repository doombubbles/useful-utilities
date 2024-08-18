using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;

namespace UsefulUtilities.Utilities;

public class ParagonSentryTargeting : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Gives the Master Builder's Green Sentry First/Last/Close/Strong/Locked targeting.";

    protected override ModSettingCategory Category => UsefulUtilitiesMod.Targeting;

    protected override string Icon => VanillaSprites.SentryGreenAAIcon;

    public override void OnNewGameModel(GameModel gameModel)
    {
        if (!Enabled) return;

        var sentry = gameModel.GetParagonTower(TowerType.EngineerMonkey)
            .FindDescendant<TowerModel>(TowerType.SentryParagonGreen);

        var dartlingGunner = gameModel.GetTower(TowerType.DartlingGunner, 4, 1);

        var attackModel = sentry.GetAttackModel();
        attackModel.AddBehavior(new RotateToTargetModel("", false, false, false, 0,
            false, false));

        var targetSelectedPointModel = dartlingGunner.GetDescendant<TargetSelectedPointModel>().Duplicate();
        targetSelectedPointModel.isOnSubTower = true;

        attackModel.AddBehavior(new TargetFirstModel("", true, true));
        attackModel.AddBehavior(new TargetLastModel("", true, true));
        attackModel.AddBehavior(new TargetCloseModel("", true, true));
        attackModel.AddBehavior(new TargetStrongModel("", true, true));

        attackModel.GetDescendant<LineEffectModel>().useRotateToPointer = false;

        sentry.towerSelectionMenuThemeId = "ActionButton";
        sentry.UpdateTargetProviders();
    }
}