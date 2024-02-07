using System.Linq;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class InGameHeroSwitch : UsefulUtility
{
    private static readonly ModSettingHotkey CycleUp = new(KeyCode.PageUp);

    private static readonly ModSettingHotkey CycleDown = new(KeyCode.PageDown);

    private static readonly ModSettingBool CycleIfPlaced = new(false)
    {
        description = "Whether to still allow cycling to different heroes if one is already placed down.",
        button = true
    };

    private static string? realSelectedHero;

    private static bool cycleDown;
    private static bool cycleUp;
    protected override bool CreateCategory => true;

    public override void OnUpdate()
    {
        if (InGame.instance == null) return;

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

    public override void OnRestart()
    {
        if (realSelectedHero != null)
        {
            ResetInventory(realSelectedHero);
        }
    }

    private static void ChangeHero(int delta)
    {
        cycleDown = cycleUp = false;

        var hero = realSelectedHero ?? InGame.instance.SelectedHero;

        var purchaseButton = ShopMenu.instance.GetTowerButtonFromBaseId(hero).GetComponent<TowerPurchaseButton>();
        if (!CycleIfPlaced &&
            purchaseButton != null &&
            purchaseButton.GetLockedState() ==
            TowerPurchaseLockState.TowerInventoryLocked)
        {
            return;
        }

        var unlockedHeroes = Game.instance.GetPlayerProfile().unlockedHeroes;

        var heroDetailsModels = InGame.instance.GetGameModel().heroSet.Select(tdm => tdm.Cast<HeroDetailsModel>());
        var heroes = heroDetailsModels as HeroDetailsModel[] ?? heroDetailsModels.ToArray();

        var index = heroes.First(hdm => hdm.towerId == hero).towerIndex;
        var newHero = "";
        while (!unlockedHeroes.Contains(newHero))
        {
            index += delta;
            index = (index + heroes.Length) % heroes.Length;
            newHero = heroes.First(hdm => hdm.towerIndex == index).towerId;
        }

        ResetInventory(newHero);
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

        realSelectedHero = newHero;
        ShopMenu.instance.RebuildTowerSet();
        foreach (var button in ShopMenu.instance.ActiveTowerButtons)
        {
            button.Cast<TowerPurchaseButton>().Update();
        }
    }
}