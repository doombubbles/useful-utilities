using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Data;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.UI;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSystem.IO;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UsefulUtilities.Utilities.InGameCharts.GroupTypes;
using UsefulUtilities.Utilities.InGameCharts.StatTypes;
namespace UsefulUtilities.Utilities.InGameCharts;

// public record struct MeterSettings(int StatType, int GroupType, bool HideMonkeyNames, bool ShowSubTowers);
public class Meters : ModWindow, IModSettings
{
    [JsonObject(MemberSerialization.OptIn)]
    [RegisterTypeInIl2Cpp(false)]
    public class MetersData(IntPtr pointer) : MonoBehaviour(pointer)
    {
        public BarChart chart = null!;
        public ModHelperPopdown statDropdown = null!;
        public ModHelperPopdown groupDropdown = null!;

        [JsonProperty]
        public int statType;
        [JsonProperty]
        public int groupType;
        [JsonProperty]
        public bool hideMonkeyNames;
        [JsonProperty]
        public bool showSubTowers;

        [HideFromIl2Cpp]
        public StatType StatType => statType;

        [HideFromIl2Cpp]
        public GroupType GroupType => groupType;
    }

    public override string Icon => VanillaSprites.ThriveStonksIcon;
    public override float IconScale => 1.2f;

    public override bool ShowTitleOnWindow => false;

    public static readonly Dictionary<ObjectId, long> DamageRoundData = new();
    public static readonly Dictionary<ObjectId, float> CashRoundData = new();
    public static readonly Queue<(int, Dictionary<ObjectId, long>)> DamageTimeData = new();

    private static DateTimeOffset lastChartUpdate = DateTimeOffset.Now;

    public override bool DontAddToStartMenu => !InGameCharts.TowerMeters;

    public override void OnUpdate()
    {
        if (TimeManager.gamePaused || TimeManager.inBetweenRounds) return;

        if (lastChartUpdate.AddMilliseconds(1000f / InGameCharts.ChartUpdateFPS) < DateTimeOffset.Now)
        {
            GetRealTimeData();
            foreach (var window in ActiveWindows)
            {
                UpdateChartData(window);
            }
            lastChartUpdate = DateTimeOffset.Now;
        }
    }

    public override void ModifyWindow(ModHelperWindow window)
    {
        var metersData = window.AddComponent<MetersData>();

        metersData.chart = window.content.Add(BarChart.Create(new Info("BarChart", InfoPreset.FillParent)
        {
            SizeDelta = new Vector2(-20, -20)
        }));

        var stats = GetContent<StatType>();
        var statDropdown = ModHelperPopdown.Create(new Info("Stat", 450, window.topBarHeight),
            new Vector2(450, 75), stats.Select(type => type.DisplayName).ToIl2CppList(),
            new Action<int>(i =>
            {
                metersData.statType = i;
                UpdateChartData(window);
            }), Vector2.up, images: stats.Select(type => type.Icon).ToIl2CppList(), autosize: true,
            menuOffset: new Vector2(0, ModHelperWindow.Margin / 2f));
        window.topLeftGroup.Add(statDropdown);
        window.ApplyWindowColor(statDropdown.menu, ModWindowColor.PanelType.Main);
        metersData.statDropdown = statDropdown;


        var groups = GetContent<GroupType>();
        var groupDropdown = ModHelperPopdown.Create(new Info("Group", 400, window.topBarHeight),
            new Vector2(400, 75), groups.Select(type => type.DisplayName).ToIl2CppList(),
            new Action<int>(i =>
            {
                metersData.groupType = i;
                UpdateChartData(window);
            }), Vector2.up, autosize: true, menuOffset: new Vector2(0, ModHelperWindow.Margin / 2f));
        window.topLeftGroup.Add(groupDropdown);
        window.ApplyWindowColor(groupDropdown.menu, ModWindowColor.PanelType.Main);
        metersData.groupDropdown = groupDropdown;
    }

    public override void ModifyOptionsMenu(ModHelperWindow window, ModHelperPopupMenu menu)
    {
        menu.AddSeparator();

        var metersData = window.GetComponent<MetersData>();

        menu.AddOption(new Info("Hide Monkey Names"), icon: VanillaSprites.NamedMonkeyIcon,
            action: new Action(() => metersData.hideMonkeyNames = !metersData.hideMonkeyNames),
            isSelected: new Func<bool>(() => metersData.hideMonkeyNames));

        menu.AddOption(new Info("Show Sub-Towers"), icon: VanillaSprites.FasterEngineeringUpgradeIcon,
            action: new Action(() => metersData.showSubTowers = !metersData.showSubTowers),
            isSelected: new Func<bool>(() => metersData.showSubTowers));
    }

    public static void ClearData()
    {
        CashRoundData.Clear();
        DamageRoundData.Clear();
        DamageTimeData.Clear();
    }

    public static void GetRealTimeData()
    {
        var towers = InGame.Bridge.Simulation.towerManager.GetTowers().ToArray();
        var time = InGame.Bridge.Simulation.roundTime.elapsed;
        var damage = towers.ToDictionary(tower => tower.Id, tower => tower.damageDealt);

        DamageTimeData.Enqueue((time, damage));

        if (DamageTimeData.Count > InGameCharts.ChartUpdateFPS * InGameCharts.DPSAverageSeconds)
        {
            DamageTimeData.Dequeue();
        }
    }

    public static void GetRoundData()
    {
        var towers = InGame.Bridge.Simulation.towerManager.GetTowers().ToArray();

        foreach (var tower in towers)
        {
            DamageRoundData[tower.Id] = tower.damageDealt;
            CashRoundData[tower.Id] = tower.cashEarned;
        }
    }

    public static void UpdateChartData(ModHelperWindow window)
    {
        var towers = InGame.Bridge.GetAllTowers().ToArray();

        var metersData = window.GetComponent<MetersData>();

        var statType = metersData.StatType;
        var groupType = metersData.GroupType;

        var data = towers
            .Where(tower => metersData.showSubTowers || !tower.tower.ParentId.IsValid)
            .GroupBy(t =>
            {
                try
                {
                    return groupType.GroupId(t);
                }
                catch (Exception e)
                {
                    ModHelper.Warning<UsefulUtilitiesMod>(e);
                    return null;
                }
            })
            .Where(group => group.Key != null)
            .Select(group =>
            {
                try
                {
                    return groupType.BarInfo(group.First(), metersData.hideMonkeyNames) with
                    {
                        Id = group.Key!,
                        Value = statType.Calculate(group)
                    };
                }
                catch (Exception e)
                {
                    ModHelper.Warning<UsefulUtilitiesMod>(e);
                    return default;
                }
            })
            .Where(info => info.Value > 0)
            .ToArray();

        try
        {
            metersData.chart.UpdateBarsFromInfo(data);
        }
        catch (Exception e)
        {
            ModHelper.Warning<UsefulUtilitiesMod>(e);
        }
    }

    public override bool SaveWindow(ModHelperWindow window, ref JObject saveData)
    {
        saveData = JObject.FromObject(window.GetComponent<MetersData>());

        return true;
    }

    public override void LoadWindow(ModHelperWindow window, JObject saveData)
    {
        var data = window.GetComponent<MetersData>();
        JsonConvert.PopulateObject(saveData.ToString(), data);

        data.statDropdown.dropdown.SetValue(data.statType);
        data.groupDropdown.dropdown.SetValue(data.groupType);
        ModHelper.Msg<UsefulUtilitiesMod>("this loaded");
    }
}