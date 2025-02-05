using System.Linq;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using UnityEngine;

#if USEFUL_UTILITIES
using BTD_Mod_Helper.Api.ModOptions;

namespace UsefulUtilities.Utilities;
#else
using static InGameHeroSwitch.InGameHeroSwitchMod;
namespace InGameHeroSwitch;
#endif

#if USEFUL_UTILITIES
public class InGameHeroSwitch : UsefulUtility
#else
public class InGameHeroSwitchUtility
#endif
{
#if USEFUL_UTILITIES
    private static readonly ModSettingHotkey CycleUp = new(KeyCode.PageUp);

    private static readonly ModSettingHotkey CycleDown = new(KeyCode.PageDown);

    protected override bool CreateCategory => true;

    private const bool CycleIfPlaced = false;

    public override void OnUpdate() => Update();
#endif

    private static bool cycleDown;
    private static bool cycleUp;

    public static void Update()
    {
        if (!InGame.instance || InGame.Bridge == null || InGame.instance.ReviewMapMode || InGame.Bridge.IsSpectatorMode || InGame.instance.GameType == GameType.Rogue) return;

        if (CycleDown.JustPressed()) cycleDown = true;
        if (CycleUp.JustPressed()) cycleUp = true;

        if (cycleDown && cycleUp)
        {
            ChangeHero(Random.RandomRangeInt(1, Game.instance.model.heroSet.Length));
        }
        else if (CycleUp.JustReleased() && cycleUp)
        {
            ChangeHero(-1);
        }
        else if (CycleDown.JustReleased() && cycleDown)
        {
            ChangeHero(1);
        }
    }

    private static string CurrentHero
    {
        get => InGame.Bridge.players[InGame.Bridge.MyPlayerNumber].hero;
        set => InGame.Bridge.players[InGame.Bridge.MyPlayerNumber].hero = value;
    }

    private static void ChangeHero(int delta)
    {
        cycleDown = cycleUp = false;

        if (string.IsNullOrEmpty(CurrentHero) ||
            !InGame.Bridge.Model.GetTowerWithName(CurrentHero).Is(out var heroModel) ||
            (InGame.instance.GetTowerInventory().GetTowerInventoryRemaining(heroModel) == 0 && !CycleIfPlaced))
        {
            return;
        }

        var unlockedHeroes = Game.instance.GetPlayerProfile().unlockedHeroes;

        var heroDetailsModels = InGame.instance.GetGameModel().heroSet.Select(tdm => tdm.Cast<HeroDetailsModel>());
        var heroes = heroDetailsModels as HeroDetailsModel[] ?? heroDetailsModels.ToArray();

        var index = heroes.First(hdm => hdm.towerId == CurrentHero).towerIndex;
        var newHero = "";
        while (!unlockedHeroes.Contains(newHero))
        {
            index += delta;
            index = (index + heroes.Length) % heroes.Length;
            newHero = heroes.First(hdm => hdm.towerIndex == index).towerId;
        }

        ResetInventory(newHero);
        CurrentHero = newHero;
        
        InGame.instance.InputManager.SetSelected(null);
    }

    private static void ResetInventory(string newHero)
    {
        var towerInventory = InGame.instance.GetTowerInventory();
        var unlockedHeroes = Game.instance.GetPlayerProfile().unlockedHeroes;
        foreach (var unlockedHero in unlockedHeroes)
        {
            towerInventory.towerMaxes[unlockedHero] = 0;
        }

        towerInventory.towerMaxes[newHero] = 1;

        RefreshShop(true);
    }

    private static void RefreshShop(bool playSound)
    {
        var disallowSelectingDifferentTowers = ShopMenu.instance.disallowSelectingDifferentTowers;
        ShopMenu.instance.disallowSelectingDifferentTowers = !playSound;
        ShopMenu.instance.RebuildTowerSet();
        ShopMenu.instance.disallowSelectingDifferentTowers = disallowSelectingDifferentTowers;
        foreach (var button in ShopMenu.instance.ActiveTowerButtons)
        {
            button.Cast<TowerPurchaseButton>().Update();
        }
    }

    [HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.MatchReady))]
    internal static class UnityToSimulation_MatchReady
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            RefreshShop(false);
        }
    }
}