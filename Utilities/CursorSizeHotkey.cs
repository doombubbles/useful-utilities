using System;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;

namespace UsefulUtilities.Utilities;

public class CursorSizeHotkey : UsefulUtility
{
    protected override bool CreateCategory => true;

    protected override string Icon => VanillaSprites.TutorialHandUp;

    private static readonly ModSettingHotkey CycleCursorSize = new();
    private static readonly ModSettingHotkey CursorSizeNormal = new();
    private static readonly ModSettingHotkey CursorSizeLarge = new();
    private static readonly ModSettingHotkey CursorSizeXLarge = new();

    public override void OnUpdate()
    {
        var currentConfig = Cursor.instance.Config;
        var newConfig = currentConfig;

        if (CursorSizeNormal.JustPressed())
        {
            newConfig = CursorConfig.Hardware;
        }

        if (CursorSizeLarge.JustPressed())
        {
            newConfig = CursorConfig.SoftwareLarge;
        }

        if (CursorSizeXLarge.JustPressed())
        {
            newConfig = CursorConfig.SoftwareXLarge;
        }

        if (CycleCursorSize.JustPressed())
        {
            newConfig++;
            if ((int) newConfig >= Enum.GetValues<CursorConfig>().Length)
            {
                newConfig = 0;
            }
        }

        if (currentConfig == newConfig) return;

        if (MenuManager.instance != null &&
            MenuManager.instance.GetCurrentMenu().Is(out HotkeysScreen hotkeysScreen))
        {
            hotkeysScreen.SetCursorConfig(newConfig);
        }
        else
        {
            Cursor.instance.Config = newConfig;
        }
    }
}