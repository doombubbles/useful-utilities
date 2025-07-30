using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Data;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Api.UI;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Models.Rounds;
using Il2CppAssets.Scripts.Simulation.Track;
using Il2CppAssets.Scripts.Simulation.Track.RoundManagers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities.InGameCharts;

public class Bloons : ModWindow, IModSettings
{
    [RegisterTypeInIl2Cpp(false)]
    public class BloonsData(IntPtr pointer) : MonoBehaviour(pointer)
    {
        public IconsBarChart chart = null!;
        public bool live;
        public int round;
        public int lastRound = -1;
        public bool lastLive;
        public bool hideBloonNames;
    }

    public override string Icon => VanillaSprites.Red;
    public override float IconScale => 2;

    public override int DefaultWidth => 1000;
    public override int DefaultHeight => 400;

    public static readonly ModSettingFloat TheScale = new(1);

    public static Spawner Spawner => InGame.Bridge.Simulation.Map.spawner;

    public static DateTimeOffset lastChartUpdate = DateTimeOffset.Now;

    public override bool DontAddToStartMenu => !InGameCharts.BloonRounds;

    public override void OnUpdate()
    {
        if (lastChartUpdate.AddMilliseconds(1000f / InGameCharts.ChartUpdateFPS) >= DateTimeOffset.Now) return;
        lastChartUpdate = DateTimeOffset.Now;

        foreach (var window in ActiveWindows)
        {
            UpdateData(window);
        }
    }

    public override void ModifyWindow(ModHelperWindow window)
    {
        var bloonsSettings = window.AddComponent<BloonsData>();

        bloonsSettings.chart = window.content.Add(IconsBarChart.Create(new Info("BarChart", InfoPreset.FillParent)
        {
            SizeDelta = new Vector2(-20, -20)
        }));

        var liveCheckbox = window.topLeftGroup.AddCheckbox(new Info("Live", 175, window.topBarHeight), true,
            VanillaSprites.SmallSquareWhiteGradient, new Action<bool>(b =>
            {
                bloonsSettings.live = b;
                window.GetDescendent<NK_TextMeshProInputField>("Round")?.SetText($"{Spawner.CurrentRound + 1}");
                UpdateData(window);
            }), ModHelperSprites.Tick);
        liveCheckbox.Check.SetInfo(liveCheckbox.initialInfo with
        {
            Size = window.topBarHeight,
            Anchor = new Vector2(0, 0.5f),
            Pivot = new Vector2(0, 0.5f),
            X = 10
        });
        liveCheckbox.AddUnCheckedIcon(VanillaSprites.CloseIcon);
        liveCheckbox.Toggle.UseBackgroundTint();

        liveCheckbox.AddText(new Info("Label", 100, window.topBarHeight)
        {
            Anchor = new Vector2(1, 0.5f),
            Pivot = new Vector2(1, 0.5f),
            X = -15
        }, "Live", ModHelperComponent.DefaultFontSize, TextAlignmentOptions.MidlineRight);


        var round = window.topLeftGroup.AddInputField(new Info("Round", 250, window.topBarHeight), "1",
            VanillaSprites.SmallSquareWhiteGradient, new Action<string>(s =>
            {
                if (string.IsNullOrEmpty("round"))
                {
                    window.GetDescendent<NK_TextMeshProInputField>("Round").SetText("1");
                    return;
                }

                if (!int.TryParse(s, out var round)) return;

                if (round < 0)
                {
                    window.GetDescendent<NK_TextMeshProInputField>("Round").SetText("1");
                    return;
                }

                bloonsSettings.round = round;
                UpdateData(window);
            }), ModHelperComponent.DefaultFontSize, TMP_InputField.CharacterValidation.Integer);
        round.Text.Text.font = Fonts.Btd6FontTitle;
        round.InputField.UseBackgroundTint();
        round.InputField.onValidateInput +=
            new Func<string, int, char, char>((s, i, c) => s.Length >= 3 || !"1234567890".Contains(c) ? '\0' : c);
        round.Viewport.SetInfo(round.Viewport.initialInfo with
        {
            AnchorX = 1,
            PivotX = 1,
            Width = 100
        });
        round.InputField.selectionColor = new Color(1, 1, 1, .75f);
        TaskScheduler.ScheduleTask(() => round.InputField.caretRectTrans.localScale = 1.25f * Vector3.one,
            () => round.Exists()?.InputField?.caretRectTrans is not null,
            () => round == null);

        round.SetActive(false);

        var label = round.AddText(new Info("Label", InfoPreset.FillParent), "Round", ModHelperComponent.DefaultFontSize,
            TextAlignmentOptions.MidlineLeft);
        label.RectTransform.offsetMin = label.RectTransform.offsetMin with
        {
            x = 15
        };
        round.GetComponent<Mask>().enabled = false;
    }

    public override void ModifyOptionsMenu(ModHelperWindow window, ModHelperPopupMenu menu)
    {
        menu.AddSeparator();

        var data = window.GetComponent<BloonsData>();
        menu.AddOption(new Info("Hide Names"),
            action: new Action(() =>
            {
                data.hideBloonNames = true;
                UpdateData(window);
            }),
            isSelected: new Func<bool>(() => data.hideBloonNames));
    }

    public override void OnUpdate(ModHelperWindow window)
    {
        var settings = window.GetComponent<BloonsData>();
        window.GetDescendent<ModHelperInputField>("Round").SetActive(!settings.live);
    }

    public static void UpdateData(ModHelperWindow window)
    {
        var chart = window.GetDescendent<IconsBarChart>();
        var settings = window.GetComponent<BloonsData>();

        IconsBarInfo[] barInfos;

        var round = Spawner.CurrentRound;

        try
        {
            if (settings.live && Spawner.roundsActive)
            {
                var emissions = Spawner.roundData.Values().SelectMany(data => data.emissions.ToArray());
                barInfos = InfoFromEmissions(emissions, InGame.Bridge.ElapsedTime, settings.hideBloonNames);
            }
            else
            {
                round = settings.live ? round : settings.round - 1;

                if (round < 0)
                {
                    barInfos = [];
                }
                else if (round != settings.lastRound || settings.lastLive != settings.live)
                {
                    var baseRoundManager = Spawner.baseRoundManager.Cast<DefaultRoundManager>();

                    Spawner.freeplayRoundManager ??= new FreeplayRoundManager(baseRoundManager.model);

                    var roundManager = round >= baseRoundManager.GetMaxRound()
                                           ? Spawner.freeplayRoundManager
                                           : Spawner.baseRoundManager;

                    var emissions = roundManager.GetRoundEmissions(round);

                    barInfos = InfoFromEmissions(emissions, -100, settings.hideBloonNames);
                }
                else
                {
                    return;
                }
            }

            chart.UpdateBarsFromInfo(barInfos);
        }
        finally
        {
            settings.lastRound = round;
            settings.lastLive = settings.live;
        }

    }

    public static IconsBarInfo[] InfoFromEmissions(IEnumerable<BloonEmissionModel> emissions, int startTime,
        bool hideNames) => emissions
        .GroupBy(emission => emission.bloon)
        .Select(group =>
        {
            var bloon = Game.instance.model.GetBloon(group.Key);
            return new IconsBarInfo
            {
                BarInfo = new BarInfo
                {
                    Id = bloon.name,
                    Icon = bloon.icon.AssetGUID,
                    Label = hideNames ? "" : BloonName(bloon),
                    Value = group.Count()
                },
                Positions = group
                    .Select(emission => (emission.time - startTime) * TheScale)
                    .ToArray(),
                Sort = bloon.danger
            };
        })
        .ToArray();


    public override void OnUnMinimized(ModHelperWindow window) => UpdateData(window);

    public static string BloonName(BloonModel bloon) => new[]
    {
        bloon.isCamo && !bloon.HasTag(BloonTag.Ddt) ? "Camo".Localize() : "",
        bloon.isGrow ? "Regrow".Localize() : "",
        bloon.isFortified ? "Fortified".Localize() : "",
        bloon.baseId.Localize()
    }.Join(delimiter: " ");

}