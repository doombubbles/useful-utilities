using System.Linq;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.RightMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.StoreMenu;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace UsefulUtilities.Utilities;

public class InGameHeroSwitch : UsefulUtility
{
    private static readonly ModSettingHotkey CycleUp = new(KeyCode.PageUp);

    private static readonly ModSettingHotkey CycleDown = new(KeyCode.PageDown);

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

    /// <summary>
    /// Gets the current selected hero
    /// </summary>
    /// <param name="towerInventory">Current Tower Inventory</param>
    /// <returns>Selected Hero id, or null if no clear selected hero, or multiple</returns>
    private static string? SelectedHero(TowerInventory towerInventory)
    {
        var heroes = InGame.Bridge.Simulation.model.heroSet.Select(hero => hero.towerId).ToList();
        return heroes.FirstOrDefault(hero =>
            towerInventory.towerMaxes.GetValueOrDefault(hero) == 1 &&
            heroes.Where(h => h != hero).All(h => towerInventory.towerMaxes.GetValueOrDefault(h) == 0)
        );
    }

    public override void OnRestart()
    {
        ShopMenu.instance.RebuildTowerSet();
    }

    private static void ChangeHero(int delta)
    {
        cycleDown = cycleUp = false;

        var hero = SelectedHero(InGame.instance.GetTowerInventory());
        
        if (hero == null) return;

        var purchaseButton = ShopMenu.instance.GetTowerButtonFromBaseId(hero).GetComponent<TowerPurchaseButton>();
        if (purchaseButton != null &&
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

        ShopMenu.instance.RebuildTowerSet();
        foreach (var button in ShopMenu.instance.ActiveTowerButtons)
        {
            button.Cast<TowerPurchaseButton>().Update();
        }
    }
}