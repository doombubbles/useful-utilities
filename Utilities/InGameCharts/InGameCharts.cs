using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity.Bridge;
using UnityEngine;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities.InGameCharts;

public class InGameCharts : UsefulUtility
{
    protected override bool CreateCategory => true;

    public override string Description => "Settings for In Game Charts";

    protected override string Icon => VanillaSprites.ThriveStonksIcon;

    public const string BarTexture = "MainBgPanelWhiteSmall";

    public static readonly ModSettingInt ChartUpdateFPS = new(20)
    {
        displayName = "Chart Update FPS",
        description = "How many times each second will charts update their displays; will never exceed framerate.",
        min = 1,
        max = 60
    };

    public static readonly ModSettingInt DPSAverageSeconds = new(3)
    {
        displayName = "DPS Average Seconds",
        description = "Number of real time seconds over which the rolling average for tower dps is calculated",
        min = 1,
        max = 15
    };

    public static readonly ModSettingBool TowerMeters = new(true)
    {
        icon = VanillaSprites.ThriveStonksIcon,
        description = "Whether to add the start menu entry for the tower damage / cash chart"
    };

    public static readonly ModSettingBool BloonRounds = new(true)
    {
        icon = VanillaSprites.Red,
        description = "Whether to add the start menu entry for the bloons / rounds chart"
    };

    internal static void ModifyScrollbar(Scrollbar scrollbar)
    {
        scrollbar.colors = scrollbar.colors with
        {
            normalColor = new Color(1, 1, 1, .25f),
            highlightedColor = new Color(1, 1, 1, .25f),
            pressedColor = new Color(1, 1, 1, .25f),
            selectedColor = new Color(1, 1, 1, .25f),
            disabledColor = new Color(1, 1, 1, .25f)
        };
        scrollbar.GetComponent<Image>().pixelsPerUnitMultiplier = 2;

        var handle = scrollbar.handleRect.GetComponent<Image>();

        handle.color = new Color(1, 1, 1, .5f);
        handle.pixelsPerUnitMultiplier = 2;
    }
}

public static class Extensions
{
    public static TowerSet TowerSet(this TowerToSimulation tower) => tower.tower.rootModel.Cast<TowerModel>().towerSet;
}