using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.Analytics;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using MelonLoader;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class FullscreenHotkey : UsefulUtility
{
    private static readonly ModSettingHotkey ToggleFullscreen = new(KeyCode.F11)
    {
        icon = GetTextureGUID<UsefulUtilitiesMod>("Fullscreen"),
        description = "Toggles Fullscreen mode. " +
                      "Will remember your previous preferred resolution when switching back and forth."
    };

    private static MelonPreferences_Entry<int> lastWindowedWidth = null!;
    private static MelonPreferences_Entry<int> lastWindowedHeight = null!;

    private static MelonPreferences_Entry<int> lastFullscreenWidth = null!;
    private static MelonPreferences_Entry<int> lastFullscreenHeight = null!;

    public override void OnRegister()
    {
        lastWindowedWidth = UsefulUtilitiesMod.Preferences.CreateEntry("lastWindowedWidth", 1600);
        lastWindowedHeight = UsefulUtilitiesMod.Preferences.CreateEntry("lastWindowedHeight", 900);
        lastFullscreenWidth =
            UsefulUtilitiesMod.Preferences.CreateEntry("lastFullscreenWidth", Display.main.systemWidth);
        lastFullscreenHeight =
            UsefulUtilitiesMod.Preferences.CreateEntry("lastFullscreenHeight", Display.main.systemHeight);
    }

    public override void OnUpdate()
    {
        if (!ToggleFullscreen.JustPressed()) return;

        var fullscreen = Screen.fullScreen;

        int newWidth;
        int newHeight;
        var newFullscreen = !fullscreen;

        if (fullscreen)
        {
            lastFullscreenWidth.Value = Screen.width;
            lastFullscreenHeight.Value = Screen.height;
            newWidth = lastWindowedWidth.Value;
            newHeight = lastWindowedHeight.Value;
        }
        else
        {
            lastWindowedWidth.Value = Screen.width;
            lastWindowedHeight.Value = Screen.height;
            newWidth = lastFullscreenWidth.Value;
            newHeight = lastFullscreenHeight.Value;
        }

        AnalyticsManager.Instance.ScreenResolution(newWidth, newHeight, newFullscreen);
        Screen.SetResolution(newWidth, newHeight, newFullscreen, Application.targetFrameRate);
        UsefulUtilitiesMod.Preferences.SaveToFile(false);

        TaskScheduler.ScheduleTask(() =>
        {
            if (MenuManager.instance != null &&
                MenuManager.instance.GetCurrentMenu().Is(out SettingsScreen settingsScreen))
            {
                settingsScreen.screenSizeDropDown.Populate();
            }
        }, () => Screen.width == newWidth);
    }
}