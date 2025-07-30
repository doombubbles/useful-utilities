using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UsefulUtilities;
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
        icon = VanillaSprites.SandboxBtn,
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
        Preferences = MelonPreferences.CreateCategory("UsefulUtilitiesPreferences");

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
        Meters.ClearRoundData();
    }

    public override void OnGameObjectsReset()
    {
        Meters.ClearRoundData();
    }
}