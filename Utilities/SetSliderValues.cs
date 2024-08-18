using System;
using System.Globalization;
using System.Linq;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Slider = UnityEngine.UI.Slider;

namespace UsefulUtilities.Utilities;

public class SetSliderValues : UsefulUtility
{
    public override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown((int) MouseButton.RightMouse)) return;


        var raycastResults = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        }, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result?.gameObject?.GetComponentsInParent<Slider>().FirstOrDefault().Is(out var slider) == true)
            {
                PopupScreen.instance.SafelyQueue(screen =>
                {
                    screen.ShowSetNamePopup("Set Value",
                        $"This slider goes between {slider!.minValue} and {slider.maxValue}",
                        new Action<string>(s => SetSliderValue(slider, float.Parse(s))),
                        slider.value.ToString(CultureInfo.CurrentCulture));
                });
                PopupScreen.instance.SafelyQueue(screen => screen.ModifyField(tmpInputField =>
                {
                    tmpInputField.characterValidation = slider!.wholeNumbers
                        ? TMP_InputField.CharacterValidation.Integer
                        : TMP_InputField.CharacterValidation.Decimal;
                }));
                return;
            }
        }
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider.wholeNumbers)
        {
            value = (float) Math.Round(value);
        }

        slider.m_Value = value;
        slider.onValueChanged.Invoke(value);
    }
}