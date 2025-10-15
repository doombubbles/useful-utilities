using System;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.Coop;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MapSelect;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class SinglePlayerCoop : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Allows you to start Co-Op lobbies even if you're the only person there. Also adds a button to the map select screen that lets you start an alternate version of Single Player co-op.";

    protected override string Icon => VanillaSprites.CoopPlayer1Icon;

    public static bool Active
    {
        get;
        set => field = InGameData.Editable.selectedCoopMode = value;
    }

    public override void OnMainMenu()
    {
        Active = false;
    }

    [HarmonyPatch(typeof(CoopLobbyScreen), nameof(CoopLobbyScreen.Update))]
    internal static class CoopLobbyScreen_Update
    {
        [HarmonyPostfix]
        public static void AllowSinglePlayerCoop(CoopLobbyScreen __instance)
        {
            if (GetInstance<SinglePlayerCoop>().Enabled)
            {
                __instance.readyBtn.enabled = true;
                __instance.readyBtn.interactable = true;
            }
        }
    }

    [HarmonyPatch(typeof(MapSelectScreen), nameof(MapSelectScreen.Open))]
    internal static class MapSelectScreen_Open
    {
        [HarmonyPostfix]
        internal static void Postfix(MapSelectScreen __instance, Il2CppSystem.Object? data)
        {
            if (!GetInstance<SinglePlayerCoop>().Enabled || data?.Unbox<bool>() == true) return;

            var bossChallenges = GetInstance<BossChallengeBadges>().Enabled;

            var button = ModHelperButton.Create(new Info(nameof(SinglePlayerCoop), 180), VanillaSprites.BlueBtnSquare,
                new Action(() =>
                {
                    BossChallengeBadges.Active = false;
                    Active = !Active;
                    __instance.mapSelectTransition.UpdateMaps();
                    MenuManager.instance.buttonClick3Sound.Play("ClickSounds");
                }));
            button.AddImage(new Info("Icon", 180), GetTextureGUID<UsefulUtilitiesMod>("P2"));
            button.SetParent(__instance.transform);

            var matcher = button.AddComponent<MatchLocalPosition>();
            matcher.transformToCopy = __instance.gameObject.GetComponentInChildrenByName<Transform>("SearchBtnObject");
            matcher.offset = new Vector3(15, bossChallenges ? -512 : -256, 0);
        }
    }

    [HarmonyPatch(typeof(MapButton), nameof(MapButton.Init))]
    internal static class MapButton_Init
    {
        [HarmonyPrefix]
        internal static void Prefix()
        {
            if (Active)
            {
                InGameData.Editable.selectedCoopMode = true;
            }
        }

        [HarmonyPostfix]
        internal static void Postfix(MapButton __instance)
        {
            if (!Active) return;

            __instance.savedData =
                Game.Player.Data.GetSavedMap(__instance.mapId, out var save) && save.IsCompatible();
            if (__instance.continueIcon != null)
            {
                __instance.continueIcon.SetActive(__instance.savedData);
            }
        }
    }

    [HarmonyPatch(typeof(MapSelectScreen), nameof(MapSelectScreen.LoadMap))]
    internal static class MapSelectScreen_LoadMap
    {
        [HarmonyPrefix]
        internal static void Prefix(ref bool __state)
        {
            if (Active)
            {
                InGameData.Editable.selectedCoopMode = false;
                __state = true;
            }
        }

        [HarmonyPostfix]
        internal static void Postfix(ref bool __state)
        {
            if (__state)
            {
                InGameData.Editable.selectedCoopMode = true;
            }
        }
    }

    [HarmonyPatch(typeof(Simulation), nameof(Simulation.GetSaveMetaData))]
    internal static class Simulation_GetSaveMetaData
    {
        [HarmonyPostfix]
        internal static void Postfix(Dictionary<string, string> metaData)
        {
            if (Active)
            {
                metaData[nameof(SinglePlayerCoop)] = "true";
            }
        }
    }

    [HarmonyPatch(typeof(Simulation), nameof(Simulation.SetSaveMetaData))]
    internal static class Simulation_SetSaveMetaData
    {
        [HarmonyPostfix]
        internal static void Postfix(Dictionary<string, string> metaData)
        {
            if (metaData.ContainsKey(nameof(SinglePlayerCoop)))
            {
                Active = true;
            }
        }
    }

    [HarmonyPatch(typeof(MapInfoManager), nameof(MapInfoManager.CompleteMode))]
    internal static class MapInfoManager_CompleteMode
    {
        [HarmonyPrefix]
        internal static void Prefix(ref bool isCoopMode)
        {
            isCoopMode |= Active;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.HasCompletedMode))]
    internal static class Btd6Player_HasCompletedMode
    {
        [HarmonyPrefix]
        internal static void Prefix(ref bool coop)
        {
            coop |= Active;
        }
    }

    [HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.HasFirstCompletedMapMmBonusBeenRewarded))]
    internal static class Btd6Player_HasFirstCompletedMapMmBonusBeenRewarded
    {
        [HarmonyPrefix]
        internal static void Prefix(ref bool isCoopMode)
        {
            isCoopMode |= Active;
        }
    }
}