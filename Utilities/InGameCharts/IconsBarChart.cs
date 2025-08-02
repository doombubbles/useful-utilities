using System;
using System.Linq;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Extensions;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
namespace UsefulUtilities.Utilities.InGameCharts;

[RegisterTypeInIl2Cpp(false)]
public class IconsBarChart(IntPtr ptr) : BarChart(ptr)
{
    public override Bar CreateBar() => IconsBar.Create(barHeight);

    private void LateUpdate()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(ScrollContent);

        foreach (var iconsBar in activeBars.Values().OfIl2CppType<IconsBar>())
        {
            iconsBar.UpdateForScrollPos();
        }
    }

    [HideFromIl2Cpp]
    public override void UpdateLayout(BarInfo[] barInfo)
    {
    }

    [HideFromIl2Cpp]
    public void UpdateBarsFromInfo(IconsBarInfo[] multiBarInfos)
    {
        UpdateBarsFromInfo(multiBarInfos.Select(info => info.BarInfo).ToArray());

        var barInfo = multiBarInfos.ToDictionary(info => info.BarInfo.Id);

        foreach (var (id, bar) in activeBars)
        {
            bar.Cast<IconsBar>().UpdateFromInfo(barInfo[id]);
        }
    }

    public static new IconsBarChart Create(Info info)
    {
        var barChart = Create<IconsBarChart>(info);

        barChart.ScrollRect.horizontal = true;
        barChart.ContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        barChart.ScrollContent.RectTransform.pivot = new Vector2(0, 1);

        return barChart;
    }
}