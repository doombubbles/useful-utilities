using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Data.Boss;
using Il2CppAssets.Scripts.Models.ServerEvents;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.Main;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MapSelect;
using Il2CppSystem;
using UnityEngine;
using Action = System.Action;
using Enum = System.Enum;

namespace UsefulUtilities.Utilities;

public class BossChallengeBadges : ToggleableUtility
{
    public static bool Active { get; set; }

    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Shows a button in the map selection screen for showing Boss Challenge completions";

    protected override string Icon => GetTextureGUID("Boss");

    [HarmonyPatch(typeof(MapSelectScreen), nameof(MapSelectScreen.Open))]
    internal static class MapSelectScreen_Open
    {
        [HarmonyPostfix]
        internal static void Postfix(MapSelectScreen __instance)
        {
            if (!GetInstance<BossChallengeBadges>().Enabled) return;

            var button = ModHelperButton.Create(new Info("BossChallenges", 180), VanillaSprites.BlueBtnSquare,
                new Action(() =>
                {
                    Active = !Active;
                    __instance.mapSelectTransition.UpdateMaps();
                    MenuManager.instance.buttonClick3Sound.Play("ClickSounds");
                }));
            button.AddImage(new Info("Icon", 150), GetTextureGUID<UsefulUtilitiesMod>("Boss"));
            button.SetParent(__instance.transform);

            var matcher = button.AddComponent<MatchLocalPosition>();
            matcher.transformToCopy = __instance.gameObject.GetComponentInChildrenByName<Transform>("SearchBtnObject");
            matcher.offset = new Vector3(15, -256, 0);
        }
    }

    [HarmonyPatch(typeof(MapButton), nameof(MapButton.Init))]
    internal static class MapButton_Init
    {
        [HarmonyPrefix]
        internal static void Prefix(MapButton __instance)
        {
            __instance.medals.SetActive(true);
            __instance.button.enabled = true;
        }

        [HarmonyPostfix]
        internal static void Postfix(MapButton __instance)
        {
            var bossBadges = __instance.gameObject.GetComponentInChildrenByName<ModHelperPanel>("BossBadges");

            __instance.medals.SetActive(!Active);
            bossBadges?.gameObject.SetActive(Active);

            if (!Active) return;

            var bosses = Enum.GetValues<BossType>();
            var bossChallenges = Game.Player.Data.bossChallengeScores ?? new();

            var bg = __instance.basicBackground;

            __instance.chimpsGoldSparkles.SetActive(false);
            __instance.chimpsHematiteSparkles.SetActive(false);

            if (bosses.All(boss => bossChallenges.TryGetValue(boss + "Elite", out var scores) &&
                                   scores.ContainsKey(__instance.mapId)))
            {
                bg = __instance.blackBackground;
                __instance.chimpsHematiteSparkles.SetActive(true);
            }
            else if (bossChallenges.Entries().Any(tuple => tuple.key.Contains("Elite") &&
                                                           tuple.value.ContainsKey(__instance.mapId)))
            {
                bg = __instance.goldBackground;
                __instance.chimpsGoldSparkles.SetActive(true);
            }
            else if (bosses.All(boss => bossChallenges.TryGetValue(boss.ToString(), out var scores) &&
                                        scores.ContainsKey(__instance.mapId)))
            {
                bg = __instance.silverBackground;
            }
            else if (bossChallenges.Values().Any(scores => scores.ContainsKey(__instance.mapId)))
            {
                bg = __instance.bronzeBackground;
            }
            __instance.mapBackground.SetSprite(bg);


            __instance.friendPanel.gameObject.SetActive(false);
            __instance.monkeyTeamsVisual.gameObject.SetActive(false);
            __instance.goldenBloonVisual.gameObject.SetActive(false);

            __instance.continueIcon.SetActive(Game.Player.Data.savedMaps.Entries().Any(tuple =>
                tuple.key.StartsWith("BC") && tuple.value.mapName == __instance.mapId));


            if (bossBadges == null)
            {
                bossBadges = __instance.gameObject.AddModHelperPanel(new Info("BossBadges")
                {
                    Position = new Vector2(0, -250),
                    SizeDelta = new Vector2(850, 140)
                }, layoutAxis: RectTransform.Axis.Horizontal, spacing: 0);

                bossBadges.LayoutGroup.childAlignment = TextAnchor.MiddleCenter;


                for (var i = 0; i < bosses.Length; i++)
                {
                    var bossType = bosses[i];

                    var panel = bossBadges.AddButton(new Info(bossType.ToString(), InfoPreset.Flex), null,
                        new Action(() =>
                        {
                            Game.Player.Data.isBossEliteSelected = true;
                            Game.Player.Data.selectedChallengeBoss = bossType;
                            Game.Player.Data.selectedChallengeBossMap = __instance.mapId;
                            MenuManager.instance.OpenMenu(SceneNames.BossEventUI,
                                new Tuple<BossEvent?, bool>(null, false));
                        }));
                    var empty = panel.AddImage(new Info(bossType.ToString(), 150)
                    {
                        Y = i % 2 * 50
                    }, VanillaSprites.ByName[bossType + "Badge"]);
                    empty.Image.color = new Color(.5f, .5f, .5f);

                    panel.AddImage(new Info(bossType + "Badge", 150)
                    {
                        Y = i % 2 * 50
                    }, VanillaSprites.ByName[bossType + "Badge"]);
                    panel.AddImage(new Info(bossType + "EliteBadge", 150)
                    {
                        Y = i % 2 * 50
                    }, VanillaSprites.ByName[bossType + "EliteBadge"]);
                }
            }


            foreach (var bossType in bosses)
            {
                var normy = bossBadges.gameObject.GetComponentInChildrenByName<Transform>(bossType + "Badge");
                var elite = bossBadges.gameObject.GetComponentInChildrenByName<Transform>(bossType + "EliteBadge");

                normy.gameObject.SetActive(bossChallenges.TryGetValue(bossType.ToString(), out var normyScores) &&
                                           normyScores != null && normyScores.ContainsKey(__instance.mapId));
                elite.gameObject.SetActive(bossChallenges.TryGetValue(bossType + "Elite", out var eliteScores) &&
                                           eliteScores != null && eliteScores.ContainsKey(__instance.mapId));
            }
        }
    }

    [HarmonyPatch(typeof(MapButton), nameof(MapButton.OnClick))]
    internal static class MapButton_OnClick
    {
        [HarmonyPrefix]
        internal static bool Prefix(MapButton __instance)
        {
            if (!Active) return true;

            Game.Player.Data.isBossEliteSelected = true;
            Game.Player.Data.selectedChallengeBossMap = __instance.mapId;

            foreach (var (key, map) in Game.Player.Data.savedMaps)
            {
                if (key.StartsWith("BC") && map.mapName == __instance.mapId &&
                    Enum.TryParse(key.Replace("BC", ""), out BossType bossType))
                {
                    Game.Player.Data.selectedChallengeBoss = bossType;
                    break;
                }
            }

            MenuManager.instance.OpenMenu(SceneNames.BossEventUI, new Tuple<BossEvent?, bool>(null, false));
            return false;
        }
    }

    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Start))]
    internal static class MainMenu_Start
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            Active = false;
        }
    }
}