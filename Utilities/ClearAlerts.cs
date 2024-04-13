using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using BTD_Mod_Helper.UI.BTD6;
using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Quests;
using Il2CppAssets.Scripts.Models.ServerEvents;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Main.HeroSelect;
using Il2CppAssets.Scripts.Unity.UI_New.Main.Home;
using Il2CppAssets.Scripts.Unity.UI_New.Main.PowersSelect;
using Il2CppAssets.Scripts.Unity.UI_New.Quests;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UsefulUtilities.Utilities;

public class ClearAlerts : ToggleableUtility
{
    protected override bool DefaultEnabled => true;

    public override string Description => "Right click on various red alert popups to clear them automatically. " +
                                          "Includes powers, instas, heroes, skins, quests, CT.";

    protected override string Icon => VanillaSprites.NoticeBtn;

    [HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
    internal class Button_OnPointerClick
    {
        [HarmonyPostfix]
        internal static void Postfix(Button __instance, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;

            var profile = Game.Player.Data;
            var changes = false;

            if (__instance.transform.parent.name == "PowersAnim")
            {
                foreach (var (_, power) in profile.powersData)
                {
                    if (power.isNew)
                    {
                        power.isNew = false;
                        changes = true;
                    }
                }

                foreach (var (_, instaTowers) in profile.instaTowers)
                {
                    foreach (var instaTower in instaTowers)
                    {
                        if (instaTower.isNew)
                        {
                            changes = true;
                            instaTower.isNew = false;
                        }
                    }
                }

                if (changes)
                {
                    __instance.GetComponentInParent<PipEventChecker>().CheckEvent(null);
                }
            }

            if (__instance.gameObject.HasComponent(out InstaTowerTypeDisplay instaTowerTypeDisplay))
            {
                var instaTowers = profile.instaTowers[instaTowerTypeDisplay.baseTowerID];

                foreach (var instaTower in instaTowers)
                {
                    if (instaTower.isNew)
                    {
                        changes = true;
                        instaTower.isNew = false;
                    }
                }

                if (changes)
                {
                    __instance.transform.FindChild("Notify").gameObject.SetActive(false);
                }
            }


            if (__instance.name == "InstaTowerMenuBtn")
            {
                foreach (var (_, instaTowers) in profile.instaTowers)
                {
                    foreach (var instaTower in instaTowers)
                    {
                        if (instaTower.isNew)
                        {
                            changes = true;
                            instaTower.isNew = false;
                        }
                    }
                }

                if (changes)
                {
                    __instance.transform.FindChild("Notify").gameObject.SetActive(false);
                }
            }

            if (__instance.transform.parent.name == "CoopAnim")
            {
                var nextCtEvent = CtEventExtensions.GetNextAvailableCtEventCached();
                if (nextCtEvent != null && profile.seenUpcomingCtEventId != nextCtEvent.id)
                {
                    profile.seenUpcomingCtEventId = nextCtEvent.id;
                    changes = true;
                    __instance.GetComponentInParent<PipEventChecker>().CheckEvent(null);
                }
            }

            if (__instance.gameObject.HasComponent(out HeroButton heroButton))
            {
                if (profile.seenNewHeroNotification.Add(heroButton.HeroId))
                {
                    changes = true;
                }

                foreach (var skinData in GameData.Instance.skinsData.SkinList.items)
                {
                    if (skinData.baseTowerName == heroButton.HeroId &&
                        profile.seenNewTowerSkinNotification.Add(skinData.name))
                    {
                        changes = true;
                    }
                }

                if (QuestTrackerManager.instance.TryGetTowerTrialQuest(heroButton.HeroId, out var quest))
                {
                    foreach (var questSaveData in profile.questsSaveData)
                    {
                        if (!questSaveData.hasSeenQuest && questSaveData.questId == quest.id)
                        {
                            questSaveData.hasSeenQuest = true;
                            changes = true;
                        }
                    }
                }

                if (changes)
                {
                    __instance.transform.FindChild("PipNotification").gameObject.SetActive(false);
                    if (heroButton.screen.SelectedHeroId == heroButton.HeroId)
                    {
                        heroButton.screen.questPip.SetActive(false);
                        foreach (var heroSkinButton in heroButton.screen.GetComponentsInChildren<HeroSkinButton>())
                        {
                            heroSkinButton.UpdateVisuals();
                        }
                    }
                }
            }

            if (__instance.gameObject.HasComponent(out NewHeroScreenNotification newHeroScreenNotification))
            {
                foreach (var heroDetails in Game.instance.model.heroSet)
                {
                    if (profile.seenNewHeroNotification.Add(heroDetails.towerId))
                    {
                        changes = true;
                    }
                }

                foreach (var skinData in GameData.Instance.skinsData.SkinList.items)
                {
                    if (profile.seenNewTowerSkinNotification.Add(skinData.name))
                    {
                        changes = true;
                    }
                }

                if (changes)
                {
                    newHeroScreenNotification.newBanner.SetActive(false);
                }
            }

            var questTabs = new Dictionary<string, QuestCategory>
            {
                { "TalesTab", QuestCategory.Tale },
                { "ChallengesTab", QuestCategory.Challenge },
                { "TutorialTab", QuestCategory.Tutorial }
            };

            if (questTabs.TryGetValue(__instance.name, out var questCategory))
            {
                foreach (var questSaveData in profile.questsSaveData)
                {
                    if (!questSaveData.hasSeenQuest &&
                        GameData.Instance.questData.TryGetQuestData(questSaveData.questId, out var questDetails) &&
                        questDetails.questCategory == questCategory)
                    {
                        questSaveData.hasSeenQuest = true;
                        changes = true;
                    }
                }

                if (changes)
                {
                    __instance.transform.FindChild("NewPip").gameObject.SetActive(false);
                    foreach (var questPanel in __instance.GetComponentInParent<QuestBrowserScreen>().questPanelList)
                    {
                        questPanel.newQuestPanel.SetActive(false);
                    }
                }
            }

            if (changes)
            {
                MenuManager.instance.buttonClickSound.Play();
                Game.Player.SaveNow();
            }
        }
    }


    [HarmonyPatch(typeof(Toggle), nameof(Toggle.OnPointerClick))]
    internal class Toggle_OnPointerClick
    {
        [HarmonyPostfix]
        internal static void Postfix(Button __instance, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;

            var profile = Game.Player.Data;
            var changes = false;

            if (__instance.gameObject.HasComponent(out PowerSelectButton powerSelectButton))
            {
                var power = profile.powersData[powerSelectButton.powerModel.name];

                if (power.isNew)
                {
                    power.isNew = false;
                    changes = true;
                }

                powerSelectButton.UpdatePowerDisplay();
            }


            if (changes)
            {
                MenuManager.instance.buttonClickSound.Play();
                Game.Player.SaveNow();
            }
        }
    }
}