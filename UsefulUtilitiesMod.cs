global using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Towers;
using MelonLoader;
using Newtonsoft.Json.Linq;
using UsefulUtilities;
using UsefulUtilities.Utilities;
using UsefulUtilities.Utilities.InGameCharts;

[assembly: MelonInfo(typeof(UsefulUtilitiesMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: HarmonyDontPatchAll]
[assembly: MelonOptionalDependencies("NAudio", "PathsPlusPlus")]

namespace UsefulUtilities;

public class UsefulUtilitiesMod : BloonsTD6Mod
{
    public static readonly Dictionary<string, UsefulUtility> UsefulUtilities = new();

    public static readonly ModSettingCategory Jukebox = new("Jukebox")
    {
        icon = VanillaSprites.JukeboxIcon,
        order = 2,
    };

    public static readonly ModSettingCategory Sandbox = new("Sandbox")
    {
        icon = VanillaSprites.SandboxBtn,
        order = 3
    };

    public static readonly ModSettingCategory TrophyStore = new("Trophy Store")
    {
        icon = VanillaSprites.TrophyStoreBtn,
        order = 4
    };

    public static MelonPreferences_Category Preferences { get; private set; } = null!;

    public override void OnApplicationStart()
    {
        Preferences = MelonPreferences.CreateCategory("5a1367fa308893747a4294d7d7958802");

        AccessTools.GetTypesFromAssembly(MelonAssembly.Assembly)
            .Where(type => !type.IsNested)
            .Do(ApplyHarmonyPatches);
    }

    public override void OnSaveSettings(JObject settings)
    {
        foreach (var usefulUtility in UsefulUtilities.Values)
        {
            usefulUtility.OnSaveSettings();
        }
    }

    public override void OnUpdate()
    {
        foreach (var usefulUtility in UsefulUtilities.Values)
        {
            usefulUtility.OnUpdate();
        }
    }

    public override void OnRoundStart()
    {
        Meters.GetRoundData();
    }

    public override void OnMatchStart()
    {
        Meters.ClearData();
        UpgradeQueueing.QueuedUpgrades.Clear();
    }

    public override void OnGameObjectsReset()
    {
        Meters.ClearData();
        UpgradeQueueing.QueuedUpgrades.Clear();
    }

    public override void OnTitleScreen()
    {
        foreach (var usefulUtility in UsefulUtilities.Values)
        {
            usefulUtility.OnTitleScreen();
        }
    }

    public override void OnMainMenu()
    {
        foreach (var usefulUtility in UsefulUtilities.Values)
        {
            usefulUtility.OnMainMenu();
        }
    }
}