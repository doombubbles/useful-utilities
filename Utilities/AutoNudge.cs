using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2Cpp;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppGeom;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

#if USEFUL_UTILITIES
using BTD_Mod_Helper.Api.Enums;

namespace UsefulUtilities.Utilities;
#else
using static AutoNudge.AutoNudgeMod;
namespace AutoNudge;
#endif

#if USEFUL_UTILITIES
public class AutoNudge : UsefulUtility
#else
public class AutoNudgeUtility
#endif
{
#if USEFUL_UTILITIES

    private static readonly ModSettingHotkey NudgeToClosest = new(KeyCode.Tab)
    {
        description = "HotKey that will automatically nudge towers to the closest valid spot while placing them.",
        icon = GetTextureGUID<UsefulUtilitiesMod>(nameof(AutoNudge))
    };

    private static readonly ModSettingHotkey NudgeLeft = new(KeyCode.LeftArrow)
    {
        icon = VanillaSprites.PS4_Dpad_Left
    };

    private static readonly ModSettingHotkey NudgeRight = new(KeyCode.RightArrow)
    {
        icon = VanillaSprites.PS4_Dpad_Right
    };

    private static readonly ModSettingHotkey NudgeUp = new(KeyCode.UpArrow)
    {
        icon = VanillaSprites.PS4_Dpad_Up
    };

    private static readonly ModSettingHotkey NudgeDown = new(KeyCode.DownArrow)
    {
        icon = VanillaSprites.PS4_Dpad_Down
    };

    private static readonly ModSettingHotkey ConfirmPlacement = new(KeyCode.Return)
    {
        icon = VanillaSprites.SelectedTick
    };

    protected override bool CreateCategory => true;

    public override void OnUpdate() => Update();
#else
    private static ModSettingHotkey NudgeToClosest => AutoNudgeHotkey;
#endif

    public static void Update()
    {
        if (!InGame.instance || InGame.Bridge == null || InGame.instance.ReviewMapMode || InGame.Bridge.IsSpectatorMode) return;

        var inputManager = InGame.instance.InputManager;

        if (!inputManager.IsInPlacementMode) return;

        if (NudgeToClosest.JustPressed())
        {
            NudgeClosest();
            if (ConfirmPlacement.IsPressed())
            {
                TaskScheduler.ScheduleTask(() => inputManager.TryPlace());
            }
        }
        else
        {
            var direction = Vector2.zero;

            if (NudgeLeft.IsPressed()) direction += new Vector2(-1, 0);
            if (NudgeRight.IsPressed()) direction += new Vector2(1, 0);
            if (NudgeUp.IsPressed()) direction += new Vector2(0, 1);
            if (NudgeDown.IsPressed()) direction += new Vector2(0, -1);

            if (direction != Vector2.zero)
            {
                NudgeDirection(direction);
            }
        }

        if (ConfirmPlacement.JustPressed())
        {
            inputManager.TryPlace();
        }
    }

    private static void NudgeClosest()
    {
        var realCursorPos = InputSystemController.MousePosition;

        var i = 0;
        while (!CanPlace(realCursorPos))
        {
            realCursorPos = InputSystemController.MousePosition + Vector2.up.Rotate(i * 10) * i / 10;
            realCursorPos = new Vector2((int) realCursorPos.x, (int) realCursorPos.y);

            if (i++ > 20000)
            {
                MelonLogger.Msg("No spot found");
                return;
            }
        }

        Mouse.current.WarpCursorPosition(realCursorPos);
    }

    private static bool CanPlace(Vector2 realCursorPos)
    {
        var inputManager = InGame.instance.InputManager;
        var bridge = InGame.instance.bridge;
        var cursorWorld = InGame.instance.GetWorldFromPointer(realCursorPos);

        return bridge.CanPlaceTowerAt(cursorWorld, inputManager.placementModel, bridge.MyPlayerNumber,
            inputManager.placementEntityId);
    }

    private static void NudgeDirection(Vector2 dir)
    {
        var screenWidth = Screen.width;
        var factor = screenWidth / 1000f;
        if (Input.GetKey(KeyCode.LeftControl))
        {
            factor *= 5;
        }

        var bonus = Random.Shared.NextSingle() < factor - (int) factor ? 1 : 0;
        var realFactor = (int) factor + bonus;
        var realCursorPos = InputSystemController.MousePosition;
        Mouse.current.WarpCursorPosition(realCursorPos + dir * realFactor);
    }
}