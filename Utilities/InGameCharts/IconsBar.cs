using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities.InGameCharts;

public record struct IconsBarInfo(BarInfo BarInfo, float[] Positions);

[RegisterTypeInIl2Cpp(false)]
public class IconsBar(IntPtr ptr) : Bar(ptr)
{
    public List<ModHelperImage> icons = [];
    public Dictionary<ModHelperImage, string?> lastIcons = [];

    public int currentTotal;
    public Vector2 lastScrollPos;
    public float lastWidth;

    public static new IconsBar Create(int height)
    {
        var multiBar = Bar.Create<IconsBar>(height);

        multiBar.icon.gameObject.Destroy();

        multiBar.icons.Add(multiBar.bar);

        multiBar.label.RectTransform.pivot = new Vector2(1, 0.5f);
        multiBar.label.Text.horizontalAlignment = HorizontalAlignmentOptions.Right;

        return multiBar;
    }

    private ScrollRect? rootScroll;

    public void UpdateForScrollPos()
    {
        rootScroll ??= root.ScrollRect;

        var content = root.RectTransform;

        /*icon.RectTransform.position = icon.RectTransform.position with
        {
            x = content.position.x - content.lossyScale.x * (content.rect.width / 2f)
        };
        */

        label.RectTransform.position = label.RectTransform.position with
        {
            x = content.position.x + content.lossyScale.x * (content.rect.width / 2f - 100 -
                                                             (rootScroll.vScrollingNeeded ? 25 : 0))
        };

        amount.RectTransform.position = amount.RectTransform.position with
        {
            x = content.position.x + content.lossyScale.x * (content.rect.width / 2f - 10 -
                                                             (rootScroll.vScrollingNeeded ? 25 : 0))
        };

        var width = root.RectTransform.rect.width;
        var scrollPos = root.ScrollRect.normalizedPosition;

        if (!Mathf.Approximately(scrollPos.x, lastScrollPos.x) || !Mathf.Approximately(scrollPos.y, lastScrollPos.y) ||
            !Mathf.Approximately(width, lastWidth))
        {
            for (var i = 0; i < currentTotal; i++)
            {
                icons[i].SetActive(IsImageVisible(icons[i]));
            }
        }

        lastWidth = width;
        lastScrollPos = scrollPos;
    }

    public bool IsImageVisible(ModHelperImage image)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(root.RectTransform, image.RectTransform.position);
    }

    public void UpdateFromInfo(IconsBarInfo iconsBarInfo)
    {
        var barInfo = iconsBarInfo.BarInfo;

        currentTotal = iconsBarInfo.Positions.Length;

        while (iconsBarInfo.Positions.Length > icons.Count)
        {
            var newBar = AddImage(new Info("Bar" + icons.Count, InfoPreset.FillParent),
                VanillaSprites.ByName[InGameCharts.BarTexture]);
            newBar.Image.type = Image.Type.Sliced;
            newBar.Image.pixelsPerUnitMultiplier = 3;

            icons.Add(newBar);

            newBar.transform.SetAsFirstSibling();
        }

        LayoutElement.minWidth = LayoutElement.preferredWidth = Math.Max(400, iconsBarInfo.Positions.Max()) +
                                                                ModHelperWindow.Margin +
                                                                amount.Text.preferredWidth +
                                                                label.Text.preferredWidth;

        for (var i = 0; i < iconsBarInfo.Positions.Length; i++)
        {
            var position = iconsBarInfo.Positions[i];
            var image = icons[i];

            image.SetActive(true);
            var rectTransform = image.RectTransform;

            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(position, 0);
            rectTransform.sizeDelta = new Vector2(barHeight, 0);
            rectTransform.localScale = 1.5f * Vector3.one;
            rectTransform.pivot = new Vector2(1, 0.5f);

            image.Image.color = Color.white;

            if (!lastIcons.TryGetValue(image, out var lastIcon) || lastIcon != barInfo.Icon)
            {
                image.Image.SetSprite(barInfo.Icon);
                lastIcons[image] = barInfo.Icon;
            }
        }

        for (var i = iconsBarInfo.Positions.Length; i < icons.Count; i++)
        {
            icons[i].SetActive(false);
        }
    }
}