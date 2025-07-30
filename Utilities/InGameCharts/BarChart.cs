using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities.InGameCharts;

[RegisterTypeInIl2Cpp(false)]
public class BarChart(IntPtr ptr) : ModHelperScrollPanel(ptr)
{
    public int barHeight = 75;

    public readonly Il2CppSystem.Collections.Generic.Dictionary<string, Bar> activeBars = new();
    public readonly Il2CppSystem.Collections.Generic.Stack<Bar> inactiveBars = new();

    public virtual Bar CreateBar() => Bar.Create(barHeight);

    public virtual double BarOrdering(Bar bar) => bar.currentValue;

    public static BarChart Create(Info info) => Create<BarChart>(info);

    public static T Create<T>(Info info) where T : BarChart
    {
        var barChart = ModHelperScrollPanel.Create<T>(info, RectTransform.Axis.Vertical, null, 2, 1);

        var horizontal = barChart.AddHorizontalScrollbar(25, VanillaSprites.SmallSquareWhiteGradient,
            VanillaSprites.SmallSquareWhiteGradient);
        InGameCharts.ModifyScrollbar(horizontal);

        var vertical = barChart.AddVerticalScrollbar(25, VanillaSprites.SmallSquareWhiteGradient,
            VanillaSprites.SmallSquareWhiteGradient);
        InGameCharts.ModifyScrollbar(vertical);


        barChart.ScrollRect.movementType = ScrollRect.MovementType.Clamped;
        barChart.ScrollRect.inertia = false;

        barChart.ScrollContent.LayoutGroup.childAlignment = TextAnchor.UpperLeft;
        barChart.ScrollContent.LayoutGroup.childForceExpandWidth = true;


        return barChart;
    }



    [HideFromIl2Cpp]
    public void UpdateBarsFromInfo(BarInfo[] barInfos)
    {
        var existingBars = new Dictionary<string, BarInfo>();
        var newBars = new Dictionary<string, BarInfo>();

        var maxValue = 0D;

        foreach (var barInfo in barInfos)
        {
            if (activeBars.ContainsKey(barInfo.Id))
            {
                existingBars.Add(barInfo.Id, barInfo);
            }
            else
            {
                newBars.Add(barInfo.Id, barInfo);
            }

            if (barInfo.Value > maxValue)
            {
                maxValue = barInfo.Value;
            }
        }

        foreach (var key in activeBars.Keys().ToArray())
        {
            if (!existingBars.ContainsKey(key))
            {
                var bar = activeBars[key];
                bar.gameObject.SetActive(false);
                inactiveBars.Push(bar);
                activeBars.Remove(key);
            }
        }

        foreach (var (id, barInfo) in existingBars)
        {
            activeBars[id].UpdateFromInfo(barInfo, maxValue);
        }

        foreach (var (id, barInfo) in newBars)
        {
            var bar = inactiveBars.Count > 0 ? inactiveBars.Pop() : CreateBar();
            bar.SetParent(ScrollContent);
            bar.root = this;

            bar.gameObject.SetActive(true);
            bar.UpdateFromInfo(barInfo, maxValue);
            activeBars[id] = bar;
        }

        foreach (var bar in inactiveBars.ToArray())
        {
            bar.transform.SetAsLastSibling();
        }

        var index = activeBars.Count - 1;

        foreach (var bar in activeBars.Values().OrderBy(BarOrdering))
        {
            bar.transform.SetSiblingIndex(index--);
        }
    }
}