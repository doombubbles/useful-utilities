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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities.InGameCharts;

public class Bloons : ModWindow, IModSettings
{
    [JsonObject(MemberSerialization.OptIn)]
    [RegisterTypeInIl2Cpp(false)]
    public class BloonsData(IntPtr pointer) : MonoBehaviour(pointer)
    {
        public IconsBarChart chart = null!;
        public BarChart compactChart = null!;
        public ModHelperCheckbox liveCheckbox = null!;
        public ModHelperInputField roundField = null!;

        [JsonProperty]
        public bool live;
        [JsonProperty]
        public int round;
        [JsonProperty]
        public bool hideBloonNames;
        [JsonProperty]
        public bool compact;

        public int lastRound = -1;
        public bool changed;
    }

    public override string Icon => VanillaSprites.Red;
    public override float IconScale => 2;

    public override int DefaultWidth => 1000;
    public override int DefaultHeight => 400;

    public override int MinimumWidth => 235;

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
        var bloonsData = window.AddComponent<BloonsData>();

        bloonsData.chart = window.content.Add(IconsBarChart.Create(new Info("Chart", InfoPreset.FillParent)
        {
            SizeDelta = new Vector2(-20, -20)
        }));

        bloonsData.compactChart = window.content.Add(BarChart.Create(new Info("CompactChart", InfoPreset.FillParent)
        {
            SizeDelta = new Vector2(-20, -20)
        }));
        bloonsData.compactChart.SetActive(false);

        var liveCheckbox = window.topLeftGroup.AddCheckbox(new Info("Live", 175, window.topBarHeight), true,
            VanillaSprites.SmallSquareWhiteGradient, new Action<bool>(b =>
            {
                bloonsData.live = b;
                bloonsData.changed = true;
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
        bloonsData.liveCheckbox = liveCheckbox;


        var roundField = window.topLeftGroup.AddInputField(new Info("Round", 250, window.topBarHeight), "1",
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

                bloonsData.round = round;
                bloonsData.changed = true;
                UpdateData(window);
            }), ModHelperComponent.DefaultFontSize, TMP_InputField.CharacterValidation.Integer);
        roundField.Text.Text.font = Fonts.Btd6FontTitle;
        roundField.InputField.UseBackgroundTint();
        roundField.InputField.onValidateInput +=
            new Func<string, int, char, char>((s, i, c) => s.Length >= 3 || !"1234567890".Contains(c) ? '\0' : c);
        roundField.Viewport.SetInfo(roundField.Viewport.initialInfo with
        {
            AnchorX = 1,
            PivotX = 1,
            Width = 100
        });
        roundField.InputField.selectionColor = new Color(1, 1, 1, .75f);
        TaskScheduler.ScheduleTask(() => roundField.InputField.caretRectTrans.localScale = 1.25f * Vector3.one,
            () => roundField.Exists()?.InputField?.caretRectTrans is not null,
            () => roundField == null);
        bloonsData.roundField = roundField;
        roundField.SetActive(false);

        var label = roundField.AddText(new Info("Label", InfoPreset.FillParent), "Round",
            ModHelperComponent.DefaultFontSize, TextAlignmentOptions.MidlineLeft);
        label.RectTransform.offsetMin = label.RectTransform.offsetMin with
        {
            x = 15
        };
        roundField.GetComponent<Mask>().enabled = false;
    }

    public override void ModifyOptionsMenu(ModHelperWindow window, ModHelperPopupMenu menu)
    {
        var data = window.GetComponent<BloonsData>();

        menu.AddSeparator();
        menu.AddOption(new Info("Compact"), icon: VanillaSprites.SmallBloonModeIcon2, action: new Action(() =>
        {
            data.compact = !data.compact;
            data.changed = true;
            UpdateData(window);
        }), isSelected: new Func<bool>(() => data.compact));
        menu.AddOption(new Info("Hide Names"), icon: VanillaSprites.NamedMonkeyIcon, action: new Action(() =>
        {
            data.hideBloonNames = !data.hideBloonNames;
            data.changed = true;
            UpdateData(window);
        }), isSelected: new Func<bool>(() => data.hideBloonNames));
    }

    public override void OnUpdate(ModHelperWindow window)
    {
        var data = window.GetComponent<BloonsData>();
        window.GetDescendent<ModHelperInputField>("Round").SetActive(!data.live);
    }

    public static void UpdateData(ModHelperWindow window)
    {
        var data = window.GetComponent<BloonsData>();

        data.chart.SetActive(!data.compact);
        data.compactChart.SetActive(data.compact);

        var round = Spawner.CurrentRound;
        IEnumerable<BloonEmissionModel> emissions;
        int startTime;

        try
        {
            if (data.live && Spawner.roundsActive && Spawner.roundData.Count > 0)
            {
                emissions = Spawner.roundData.Values().SelectMany(rd => rd.emissions.ToArray());
                startTime = InGame.Bridge.ElapsedTime;
            }
            else
            {
                round = data.live ? Spawner.roundsActive ? round + 1 : round : data.round - 1;

                if (round < 0)
                {
                    emissions = [];
                    startTime = 0;
                }
                else if (round != data.lastRound || data.changed)
                {
                    var baseRoundManager = Spawner.baseRoundManager.Cast<DefaultRoundManager>();

                    Spawner.freeplayRoundManager ??= new FreeplayRoundManager(baseRoundManager.model);

                    var roundManager = round >= baseRoundManager.GetMaxRound()
                                           ? Spawner.freeplayRoundManager
                                           : Spawner.baseRoundManager;

                    emissions = roundManager.GetRoundEmissions(round);
                    startTime = -100;
                }
                else
                {
                    return;
                }
            }

            if (data.compact)
            {
                data.compactChart.UpdateBarsFromInfo(GetInfo(emissions, data.hideBloonNames));
            }
            else
            {
                data.chart.UpdateBarsFromInfo(GetInfo(emissions, startTime, data.hideBloonNames));
            }
        }
        finally
        {
            data.lastRound = round;
            data.changed = false;
        }
    }


    public static IconsBarInfo[] GetInfo(IEnumerable<BloonEmissionModel> emissions, int startTime, bool hideNames) =>
        emissions
            .GroupBy(emission => emission.bloon)
            .Select(group => new IconsBarInfo
            {
                BarInfo = GetInfo(group.Key, group.Count(), hideNames),
                Positions = group
                    .Select(emission => emission.time - startTime)
                    .ToArray(),
            })
            .ToArray();

    public static BarInfo[] GetInfo(IEnumerable<BloonEmissionModel> emissions, bool hideNames) =>
        emissions
            .GroupBy(emission => emission.bloon)
            .Select(group => GetInfo(group.Key, group.Count(), hideNames))
            .ToArray();

    public static BarInfo GetInfo(string bloonId, int count, bool hideNames)
    {
        var bloon = Game.instance.model.GetBloon(bloonId);
        return new BarInfo
        {
            Id = bloon.name,
            Icon = bloon.icon.AssetGUID,
            Label = hideNames ? "" : BloonName(bloon),
            Color = new Color(0, 0, 0, 0),
            Sort = bloon.danger,
            Value = count
        };
    }

    public static string BloonName(BloonModel bloon) => new[]
    {
        bloon.isCamo && !bloon.HasTag(BloonTag.Ddt) ? "Camo".Localize() : "",
        bloon.isGrow ? "Regrow".Localize() : "",
        bloon.isFortified ? "Fortified".Localize() : "",
        bloon.baseId.Localize()
    }.Join(delimiter: " ");

    public override void OnUnMinimized(ModHelperWindow window) => UpdateData(window);

    public override bool SaveWindow(ModHelperWindow window, ref JObject saveData)
    {
        saveData = JObject.FromObject(window.GetComponent<BloonsData>());
        return true;
    }

    public override void LoadWindow(ModHelperWindow window, JObject saveData)
    {
        var data = window.GetComponent<BloonsData>();
        JsonConvert.PopulateObject(saveData.ToString(), data);

        data.liveCheckbox.SetChecked(data.live);
        data.roundField.SetText(data.round.ToString());
    }
}