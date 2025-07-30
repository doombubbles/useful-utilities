using System;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
namespace UsefulUtilities.Utilities.InGameCharts;

public record struct BarInfo(string Id, double Value, string Label, string? Icon, Color? Color);

[RegisterTypeInIl2Cpp(false)]
public class Bar(IntPtr ptr) : ModHelperPanel(ptr)
{
    public ModHelperImage bar = null!;
    public ModHelperImage icon = null!;
    public ModHelperText label = null!;
    public ModHelperText amount = null!;

    public ModHelperScrollPanel root = null!;

    private string? lastIcon;

    public double currentValue;

    public int barHeight;

    public static Bar Create(int height) => Create<Bar>(height);

    public static T Create<T>(int height) where T : Bar
    {
        var bar = Create<T>(new Info("BarPanel")
        {
            Height = height
        });
        bar.barHeight = height;

        bar.bar = bar.AddImage(new Info("Bar", InfoPreset.FillParent),
            VanillaSprites.ByName[InGameCharts.BarTexture]);
        bar.bar.Image.type = Image.Type.Sliced;
        bar.bar.Image.pixelsPerUnitMultiplier = 3;

        bar.label = bar.AddText(new Info("Label")
        {
            AnchorMin = new Vector2(0, 0),
            AnchorMax = new Vector2(0.75f, 1),
            Pivot = new Vector2(0, 0.5f),
            X = height + 10
        }, "", 50, TextAlignmentOptions.MidlineLeft);
        bar.label.Text.EnableAutoSizing(50, 25);
        bar.label.Text.lineSpacing = 0;

        bar.amount = bar.AddText(new Info("Amount")
        {
            AnchorMin = new Vector2(0.75f, 0),
            AnchorMax = new Vector2(1, 1),
            Pivot = new Vector2(1, 0.5f),
            X = -10
        }, "", 50, TextAlignmentOptions.MidlineRight);

        bar.icon = bar.AddImage(new Info("Icon", height)
        {
            Anchor = new Vector2(0, 0.5f),
            Pivot = new Vector2(0, .5f)
        }, "");

        return bar;
    }

    [HideFromIl2Cpp]
    public void UpdateFromInfo(BarInfo barInfo, double maxValue)
    {
        name = barInfo.Id;

        bar.RectTransform.anchorMax = new Vector2((float) (barInfo.Value / maxValue), 1);
        bar.Image.color = barInfo.Color ?? Color.white;

        if (label.Text.text != barInfo.Label)
        {
            label.SetText(barInfo.Label);
        }

        currentValue = barInfo.Value;
        var formattedValue = FormatValue(barInfo.Value);
        if (amount.Text.text != formattedValue)
        {
            amount.SetText(formattedValue);
        }

        if (icon != null)
        {
            var hasIcon = !string.IsNullOrEmpty(barInfo.Icon);
            icon.SetActive(hasIcon);
            if (hasIcon && barInfo.Icon != lastIcon)
            {
                icon.Image.SetSprite(barInfo.Icon);
            }
            lastIcon = barInfo.Icon;
        }
    }

    public static string FormatValue(double value) => value switch
    {
        >= 1e15 => $"{value:G3}",
        >= 1e12 => $"{value / 1e12:G3}T",
        >= 1e9 => $"{value / 1e9:G3}B",
        >= 1e6 => $"{value / 1e6:G3}M",
        >= 1e4 => $"{value / 1e3:G3}K",
        _ => $"{value:N0}"
    };
}