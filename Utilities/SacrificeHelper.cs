using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Data.ParagonData;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu.TowerSelectionMenuThemes;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Unity.UI_New.Utils;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using static BTD_Mod_Helper.Api.Enums.VanillaSprites;


#if USEFUL_UTILITIES
namespace UsefulUtilities.Utilities;
#else
namespace SacrificeHelper;
#endif

#if USEFUL_UTILITIES
using BTD_Mod_Helper.Api.Data;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

public class SacrificeHelper : UsefulUtility
#else
using BTD_Mod_Helper.Api.Helpers;
using Il2CppAssets.Scripts.Unity;
using BTD_Mod_Helper.Api.Data;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;

public class SacrificeHelperUtility : IModSettings
#endif
{
#if USEFUL_UTILITIES
    protected override string Icon => BloodSacrificeAA;

    protected override bool CreateCategory => true;
#endif

    private static readonly ModSettingBool TempleSacrificeInfo = new(true)
    {
        description =
            "Shows indicators for Sun Temple and True Sun God sacrifices for how much of each tower category is in range.",
        icon = SunTempleIcon
    };

    private static readonly ModSettingBool ParagonSacrificeInfo = new(true)
    {
        description =
            "Shows indicators for Paragon sacrifices for how what degree the paragon will be, " +
            "and also how much power is coming from each source.",
        icon = PerfectParagonIcon
    };

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpdateTower))]
    internal static class TowerSelectionMenu_UpdateTower
    {
        [HarmonyPostfix]
        private static void Postfix(TowerSelectionMenu __instance)
        {
            var themeManager = __instance.themeManager;
            var currentTheme = themeManager.CurrentTheme;

            if (currentTheme == null) return;

            var ui = currentTheme.GetComponent<SacrificeHelperUI>();
            if (ui != null)
            {
                ui.TowerInfoChanged();
            }
        }
    }


    [HarmonyPatch(typeof(MenuThemeManager), nameof(MenuThemeManager.SetTheme))]
    internal static class MenuThemeManager_SetTheme
    {
        [HarmonyPostfix]
        private static void Postfix(MenuThemeManager __instance, BaseTSMTheme newTheme)
        {
            if (!__instance.selectionMenu.Is(out TowerSelectionMenu menu)) return;

            var ui = newTheme.GetComponent<SacrificeHelperUI>();
            if (ui == null)
            {
                ui = newTheme.gameObject.AddComponent<SacrificeHelperUI>();
                ui.Initialise(menu);
                ui.TowerInfoChanged();
            }
        }
    }


    [RegisterTypeInIl2Cpp(false)]
    public class SacrificeHelperUI : MonoBehaviour
    {
        public const int InfoWidth = 500;
        public const int InfoHeight = 100;

#if USEFUL_UTILITIES
        private static bool showingExtraTempleInfo;
#else
        // ReSharper disable once InconsistentNaming
        private static bool showingExtraTempleInfo
        {
            get => !SacrificeHelperMod.templeSacrificesOff;
            set => SacrificeHelperMod.templeSacrificesOff = !value;
        }
#endif
        private static bool showingExtraParagonInfo;

        public TowerSelectionMenu menu = null!;

        private ModHelperPanel paragonStuff = null!;
        private ModHelperButton degreeButton = null!;
        private ModHelperText degreeText = null!;
        private ModHelperPanel extraParagonInfo = null!;
        private Il2CppSystem.Collections.Generic.List<ModHelperText> paragonDetails = null!;

        private ModHelperPanel templeStuff = null!;
        private ModHelperButton sacrificeToggle = null!;
        private ModHelperPanel extraSacrificeInfo = null!;
        private Il2CppSystem.Collections.Generic.List<ModHelperText> sacrificeTowerSets = null!;

        public SacrificeHelperUI(IntPtr ptr) : base(ptr)
        {
        }

        public void Initialise(TowerSelectionMenu towerSelectionMenu)
        {
            menu = towerSelectionMenu;

            if (ParagonSacrificeInfo)
            {
                CreateParagonStuff();
            }

            if (TempleSacrificeInfo)
            {
                CreateTempleStuff();
            }
        }

        private void CreateParagonStuff()
        {
            paragonStuff = gameObject.AddModHelperPanel(new Info("ParagonStuff", InfoPreset.FillParent));
            degreeButton = paragonStuff.AddButton(new Info("ParagonButton", 375, -75, 135),
                UpgradeContainerParagon, new Action(() =>
                {
                    showingExtraParagonInfo = !showingExtraParagonInfo;
                    UpdateExtraInfo();
                }));
            degreeText = degreeButton.AddText(new Info("DegreeText", InfoPreset.FillParent), "0", 60);

            extraParagonInfo = paragonStuff.AddPanel(new Info("ExtraParagonInfo", 50, -50, InfoWidth)
            {
                PivotY = 1
            }, null, RectTransform.Axis.Vertical);
            paragonDetails = ParagonDetail.CreateTexts(extraParagonInfo).ToIl2CppList();
        }


        private void CreateTempleStuff()
        {
            templeStuff = gameObject.AddModHelperPanel(new Info("TempleStuff", InfoPreset.FillParent));
            sacrificeToggle = templeStuff.AddButton(new Info("TempleButton", 375, -75, 120),
                NotificationYellow, new Action(() =>
                {
                    showingExtraTempleInfo = !showingExtraTempleInfo;
#if !USEFUL_UTILITIES
                    UpdateUpgradeCosts();
#endif
                    UpdateExtraInfo();
                }));
            sacrificeToggle.AddImage(new Info("SacrificeIcon", 80), BuffIconBloodSacrifice);

            extraSacrificeInfo = templeStuff.AddPanel(new Info("ExtraSacrificeInfo", 50, -50, InfoWidth)
            {
                PivotY = 1
            }, null, RectTransform.Axis.Vertical);
            var modHelperTexts = TowerSetType.All.ToList()
                .Select(s => CreateInfoLine(extraSacrificeInfo, s, $"MainMenuUiAtlas[{s}Btn]"));
            sacrificeTowerSets = modHelperTexts.ToIl2CppList();
        }

        public void TowerInfoChanged()
        {
            var tower = menu.selectedTower;
            if (tower == null)
            {
                MelonLogger.Warning("Couldn't update Paragon Helper UI because tower was null");
                return;
            }


            if (ParagonSacrificeInfo)
            {
                UpdateParagonStuff(tower);
            }

            if (TempleSacrificeInfo)
            {
                UpdateTempleStuff(tower);
            }

            UpdateExtraInfo();
        }

        private void UpdateParagonStuff(TowerToSimulation tower)
        {
            var canUpgradeToParagon = tower.CanUpgradeToParagon(true);
            paragonStuff.SetActive(canUpgradeToParagon);
            if (canUpgradeToParagon)
            {
                var degree = Utils.GetParagonDegree(tower, out var investmentInfo);
                degreeText.SetText($"{degree}");
                degreeText.Text.color = degree >= 100 ? Color.green : Color.white;

                var details = paragonDetails.ToList();
                for (var i = 0; i < ParagonDetail.AllDetails.Count; i++)
                {
                    ParagonDetail.AllDetails[i].Update(details[i], investmentInfo);
                }
            }
        }

        private void UpdateTempleStuff(TowerToSimulation tower)
        {
            var canUpgradeToTemple = tower.Def.upgrades?.Any(model => model.upgrade == UpgradeType.SunTemple) == true;
            var canUpgradeToGod = tower.Def.upgrades?.Any(model => model.upgrade == UpgradeType.TrueSunGod) == true;
            templeStuff.SetActive(canUpgradeToTemple || canUpgradeToGod);
            if (canUpgradeToTemple || canUpgradeToGod)
            {
                var worths = Utils.GetTowerWorths(tower.tower);
                var colors = Utils.GetColors(worths, canUpgradeToGod);

                var towerSets = TowerSetType.All.ToList();
                var details = sacrificeTowerSets.ToList();
                for (var i = 0; i < details.Count; i++)
                {
                    var towerSet = towerSets[i];
                    var detail = details[i];

                    detail.SetText($"${worths[towerSet]:N0}");
                    detail.Text.color = colors[towerSet];
                }
            }
        }

        private void UpdateExtraInfo()
        {
            if (ParagonSacrificeInfo)
            {
                extraParagonInfo.SetActive(showingExtraParagonInfo);
            }

            if (TempleSacrificeInfo)
            {
                extraSacrificeInfo.SetActive(showingExtraTempleInfo);
            }
        }

#if !USEFUL_UTILITIES
        private void UpdateUpgradeCosts()
        {
            var gameModel = InGame.instance.GetGameModel();
            var templeUpgrade = gameModel.GetUpgrade(UpgradeType.SunTemple);
            var godUpgrade = gameModel.GetUpgrade(UpgradeType.TrueSunGod);
            if (SacrificeHelperMod.templeSacrificesOff)
            {
                Utils.ModifyTemple(templeUpgrade);
                Utils.ModifyGod(godUpgrade);
            }
            else
            {
                Utils.DefaultTemple(templeUpgrade);
                Utils.DefaultGod(godUpgrade);
            }

            for (var i = 0; i < menu.upgradeButtons.Count; i++)
            {
                var upgradeButton = menu.upgradeButtons[i];
                upgradeButton.UpdateCost();
                upgradeButton.UpdateVisuals(i, false);
            }
        }
#endif

        private static ModHelperText CreateInfoLine(ModHelperPanel parent, string id, string icon)
        {
            var panel = parent.AddPanel(new Info(id, InfoWidth, InfoHeight), null, RectTransform.Axis.Horizontal, 25);
            panel.AddImage(new Info("Icon", InfoHeight), icon);
            var text = panel.AddText(new Info("Amount", InfoWidth - InfoHeight - 50, InfoHeight), "", 69);
            text.Text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }
    }


    public class ParagonDetail
    {
        public static List<ParagonDetail> AllDetails { get; }

        static ParagonDetail()
        {
            AllDetails = new List<ParagonDetail>
            {
                new("Money Spent", VanillaSprites.CoinIcon, info => info.powerFromMoneySpent,
                    model => model.maxPowerFromMoneySpent),
                new("Pops / Generated Cash", VanillaSprites.PopIcon, info => info.powerFromPops,
                    model => model.maxPowerFromPops),
                new("Non Tier 5 Upgrades Purchased", VanillaSprites.UpgradeContainerGrey,
                    info => info.powerFromNonTier5Tiers, model => model.maxPowerFromNonTier5Count,
                    image => image.AddText(new Info("Text", InfoPreset.FillParent), "<5", 69)),
                new("Having Tier 5 Towers", VanillaSprites.UpgradeContainerTier5, info => info.powerFromTier5Count,
                    model => model.maxPowerFromTier5Count,
                    image => image.AddText(new Info("Text", InfoPreset.FillParent), "5", 69)),
                new("Geraldo Totems", VanillaSprites.ParagonPowerTotem, info => info.powerFromBonus)
            };
        }

        public string Name { get; }
        public string Icon { get; }
        public Func<ParagonTower.InvestmentInfo, float> Current { get; }
        public Func<ParagonDegreeDataModel, float>? Max { get; }
        public Action<ModHelperComponent>? ModifyIcon { get; }
        private float current;
        private float max;

        public ParagonDetail
        (
            string name, string icon, Func<ParagonTower.InvestmentInfo, float> current,
            Func<ParagonDegreeDataModel, float>? max = null, Action<ModHelperComponent>? modifyIcon = null
        )
        {
            Name = name;
            Icon = icon;
            Current = current;
            Max = max;
            ModifyIcon = modifyIcon;
        }

        public void Update(ModHelperText text, ParagonTower.InvestmentInfo investmentInfo)
        {
            current = Current(investmentInfo);
            max = Max?.Invoke(InGame.instance.GetGameModel().paragonDegreeDataModel) ?? -1;
            text.SetText($"{current:N0}");
            text.Text.color = max > 0 && current >= max ? Color.green : Color.white;
        }

        public static IEnumerable<ModHelperText> CreateTexts(ModHelperPanel extraParagonInfo) =>
            AllDetails.Select(detail => detail.CreateInfoLine(extraParagonInfo));

        private ModHelperText CreateInfoLine(ModHelperPanel parent)
        {
            var panel = parent.AddPanel(new Info(Name, SacrificeHelperUI.InfoWidth, SacrificeHelperUI.InfoHeight), null,
                RectTransform.Axis.Horizontal, 25);
            var icon = panel.AddButton(new Info("Icon", SacrificeHelperUI.InfoHeight), Icon, new Action(() =>
            {
                var message = $"You are getting {current:N0} Paragon Power from {Name}.";
                if (max > 0)
                {
                    message += $" You are {current / max:P0} of the way to the maximum of {max:N0}.";
                }

                PopupScreen.instance.SafelyQueue(screen => screen.ShowOkPopup(message));
            }));
            ModifyIcon?.Invoke(icon);
            var text = panel.AddText(
                new Info("Amount", SacrificeHelperUI.InfoWidth - SacrificeHelperUI.InfoHeight - 50,
                    SacrificeHelperUI.InfoHeight), "", 69);
            text.Text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }
    }


    public static class Utils
    {
#if !USEFUL_UTILITIES
        public static void DefaultTemple(UpgradeModel upgradeModel)
        {
            upgradeModel.confirmation = UpgradeType.SunTemple;
            var baseCost = Game.instance.model.GetUpgrade(upgradeModel.name).cost;
            upgradeModel.cost = CostHelper.CostForDifficulty(baseCost, InGame.instance);
        }

        public static void ModifyTemple(UpgradeModel upgradeModel)
        {
            upgradeModel.confirmation = "";
            var baseCost = Game.instance.model.GetUpgrade(upgradeModel.name).cost;
            var mod = SacrificeHelperMod.TempleAlternateCostMod;
            upgradeModel.cost = CostHelper.CostForDifficulty((int) (baseCost * mod), InGame.instance);
        }

        public static void DefaultGod(UpgradeModel upgradeModel)
        {
            upgradeModel.confirmation = UpgradeType.TrueSunGod;
            var baseCost = Game.instance.model.GetUpgrade(upgradeModel.name).cost;
            upgradeModel.cost = CostHelper.CostForDifficulty(baseCost, InGame.instance);
        }

        public static void ModifyGod(UpgradeModel upgradeModel)
        {
            upgradeModel.confirmation = "";
            var baseCost = Game.instance.model.GetUpgrade(upgradeModel.name).cost;
            var mod = SacrificeHelperMod.GodAlternateCostMod;
            upgradeModel.cost = CostHelper.CostForDifficulty((int) (baseCost * mod), InGame.instance);
        }

#endif

        public static Dictionary<string, Color> GetColors(Dictionary<string, float> worths, bool god)
        {
            var ret = new Dictionary<string, Color>();
            if (!god)
            {
                var worst = "";
                var min = float.MaxValue;
                foreach (var key in worths.Keys)
                {
                    if (worths[key] < min)
                    {
                        worst = key;
                        min = worths[key];
                    }
                }

                ret[worst] = Color.red;
            }


            foreach (var key in worths.Keys)
            {
                if (ret.ContainsKey(key))
                {
                    continue;
                }

                var worth = worths[key];
                var color = Color.red;
                if (worth > 50000)
                {
                    color = Color.green;
                }
                else if (key == "Magic")
                {
                    if (worth > 1000)
                    {
                        color = Color.white;
                    }
                }
                else if (worth > 300)
                {
                    color = Color.white;
                }

                ret[key] = color;
            }

            return ret;
        }

        public static bool IsValidTower(Tower tower) => !tower.towerModel.isPowerTower &&
                                                        !tower.towerModel.isGeraldoItem &&
                                                        !tower.towerModel.IsBeastHandlerPet &&
                                                        !tower.towerModel.isParagon &&
                                                        tower.towerModel.baseId != "TempleBase-TempleBase";

        public static Dictionary<string, float> GetTowerWorths(Tower tower) =>
            TowerSetType.All.ToDictionary(s => s, s => GetTowerSetWorth(s, tower));

        private static float GetTowerSetWorth(string towerSet, Tower tower) => InGame.instance.GetTowerManager()
            .GetTowersInRange(tower.Position, tower.towerModel.range)
            .ToList()
            .Where(t => t.towerModel.towerSet.ToString() == towerSet && t.Id != tower.Id && IsValidTower(t))
            .Sum(t => t.worth);

        private static ParagonTower FakeParagonTower(Tower tower) => new()
        {
            Sim = tower.Sim,
            entity = tower.entity,
            model = tower.model,
            tower = tower,
            isActive = true,
            activeAt = -1
        };

        public static long GetParagonDegree(TowerToSimulation tower, out ParagonTower.InvestmentInfo investmentInfo,
            float bonus = 0)
        {
            var gameModel = tower.sim.Model;
            var degreeDataModel = gameModel.paragonDegreeDataModel;

            var paragonCost = tower.IsParagon
                ? gameModel.GetParagonUpgradeForTowerId(tower.Def.baseId).cost
                : tower.GetUpgradeCost(0, 6, -1, true);
            var powerFromMoneySpent = bonus * degreeDataModel.moneySpentOverX /
                                      ((1 + degreeDataModel.paidContributionPenalty) * paragonCost);

            var bonusInvestment = new ParagonTower.InvestmentInfo
            {
                powerFromMoneySpent = bonus * degreeDataModel.moneySpentOverX /
                                      ((1 + degreeDataModel.paidContributionPenalty) * paragonCost)
            };

            if (tower.tower.entity.GetBehavior<ParagonTower>().Is(out var paragonTower))
            {
                investmentInfo = paragonTower.investmentInfo with
                {
                    totalInvestment = paragonTower.investmentInfo.totalInvestment + powerFromMoneySpent
                };
            }
            else
            {
                paragonTower = FakeParagonTower(tower.tower);
                paragonTower.upgradeCost = paragonCost;

                var index = 0;
                investmentInfo = InGame.instance.GetAllTowerToSim()
                    .Where(tts =>
                        tts.Def.baseId == tower.Def.baseId || tts.Def.GetChild<ParagonSacrificeBonusModel>() != null)
                    .OrderBy(tts => paragonTower.GetTowerInvestment(tts.tower).totalInvestment)
                    .Select(tts => paragonTower.GetTowerInvestment(tts.tower, tts.Def.tier >= 5 ? index++ : 3))
                    .Aggregate(bonusInvestment, paragonTower.CombineInvestments);
            }


            var degree = 0L;

            if (ModHelper.HasMod("Paragonomics", out var paragonomics))
            {
                degree = (int) paragonomics.Call("GetDegree", investmentInfo.totalInvestment);
            }
            else
            {
                while (investmentInfo.totalInvestment >= degreeDataModel.powerDegreeRequirements[(int) degree])
                {
                    degree++;
                    if (degree == degreeDataModel.powerDegreeRequirements.Length)
                    {
                        break;
                    }
                }
            }

            return degree;
        }
    }

    [HarmonyPatch(typeof(ParagonConfirmationPopup), nameof(ParagonConfirmationPopup.UpdateCurrentInvestment))]
    internal static class ParagonConfirmationPopup_UpdateCurrentInvestment
    {
        [HarmonyPostfix]
        private static void Postfix(ParagonConfirmationPopup __instance, float current)
        {
            var tower = TowerSelectionMenu.instance.selectedTower;
            var bonus = current;
            var upgradeCost = InGame.Bridge.Model.GetParagonUpgradeForTowerId(tower.Def.baseId).cost;
            if (__instance.upgradeCost != 0 && InGame.Bridge.GetCash() < upgradeCost)
            {
                // Handle Paragonomics negative degree
                bonus += __instance.upgradeCost;
                bonus -= upgradeCost;
            }

            var degree = Utils.GetParagonDegree(tower, out _, bonus);

            var text = __instance.transform.GetComponentFromChildrenByName<NK_TextMeshProUGUI>("DegreeText");
            text.SetText(degree.ToString());
            text.color = Color.white;
            if (degree >= 100 &&
                (bool?) ModHelper.GetMod("Paragonomics")?.ModSettings["NoDegreeLimit"].GetValue() != true)
            {
                text.color = Color.green;
            }
        }
    }

    [HarmonyPatch(typeof(ParagonConfirmationPopup), nameof(ParagonConfirmationPopup.Init),
        typeof(Il2CppSystem.Action<double>), typeof(Il2CppSystem.Action), typeof(int), typeof(int), typeof(int),
        typeof(PopupScreen.BackGround), typeof(Popup.TransitionAnim))]
    internal static class ParagonConfirmationPopup_Init
    {
        [HarmonyPostfix]
        private static void Postfix(ParagonConfirmationPopup __instance)
        {
            var tower = TowerSelectionMenu.instance.selectedTower;
            var bonus = 0;
            var upgradeCost = InGame.Bridge.Model.GetParagonUpgradeForTowerId(tower.Def.baseId).cost;
            if (__instance.upgradeCost != 0 && InGame.Bridge.GetCash() < upgradeCost)
            {
                // Handle Paragonomics negative degree
                bonus += __instance.upgradeCost;
                bonus -= upgradeCost;
            }

            var degree = Utils.GetParagonDegree(tower, out _, bonus);

            var mainObject = __instance.animator.gameObject;

            var indicator =
                ModHelperImage.Create(new Info("DegreeIndicator", 0, -450, 250, 250), UpgradeContainerParagon);
            mainObject.AddModHelperComponent(indicator);
            indicator.AddText(new Info("DegreeText", InfoPreset.FillParent), degree.ToString(), 100f);
        }
    }
}