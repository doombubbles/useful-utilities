using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.HeroInGame;
using Il2CppAssets.Scripts.Unity.UI_New.Main.HeroSelect;
using Il2CppAssets.Scripts.Unity.UI_New.Main.PowersSelect;
using Il2CppAssets.Scripts.Unity.UI_New.Upgrade;
using Il2CppNinjaKiwi.LiNK.Client.LiNKAccountControllers;
using Il2CppTMPro;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using TaskScheduler = BTD_Mod_Helper.Api.TaskScheduler;

#if DEBUG
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BTD_Mod_Helper.Api.ModMenu;
using Il2CppAssets.Scripts.Unity;
using Il2CppNinjaKiwi.Common;
using Newtonsoft.Json;
#endif

namespace UsefulUtilities.Utilities;

public class WikiLinks : ToggleableUtility
{
    private const string WikiUrl = "https://bloons.fandom.com/wiki/";
    private const string WikiLinksJSON = "wiki_links.json";
    private const string CleanWikiPageJS = "clean-wiki-page.js";

    private static readonly Dictionary<string, string> WikiLinkTable = new();
    private static string cleanWikiPageScript = null!;

    private static readonly ModSettingBool EmbeddedBrowser = new(false)
    {
        description = "Open links in embedded BTD6 browser rather than the external browser",
        icon = VanillaSprites.GwendolinFirefoxPetIcon
    };

    private static readonly ModSettingBool UnderlineLinks = new(true)
    {
        description = "Toggles the underlining of text that has become links to be more prominently visible"
    };

    protected override bool CreateCategory => true;
    protected override string Icon => VanillaSprites.InfoIcon;
    protected override bool DefaultEnabled => true;

    public override string Description =>
        "Turns the names within the Towers/Upgrads/Heroes screen into links to their BTD6 wiki page";

    public override async void OnLoad()
    {
        base.OnLoad();

        if (ModHelper.IsEpic)
        {
            mod.ModSettings.Remove(nameof(EmbeddedBrowser));
        }

        await using var jsonStream = mod.MelonAssembly.Assembly.GetEmbeddedResource(WikiLinksJSON);
        using var jsonReader = new StreamReader(jsonStream);
        var jsonText = await jsonReader.ReadToEndAsync();
        var jobject = JObject.Parse(jsonText);

        foreach (var (key, value) in jobject)
        {
            WikiLinkTable[key] = value?.ToString()!;
        }

        await using var jsStream = mod.MelonAssembly.Assembly.GetEmbeddedResource(CleanWikiPageJS);
        using var jsReader = new StreamReader(jsStream);
        cleanWikiPageScript = await jsReader.ReadToEndAsync();
    }

    private static void Setup(TextMeshProUGUI text, Func<string> getName, float? lineSpacing = null)
    {
        if (!GetInstance<WikiLinks>().Enabled || !text.IsActive()) return;

        var current = getName();
        var hasLink = WikiLinkTable.ContainsKey(current);

        if (!text.gameObject.HasComponent(out Button button))
        {
            button = text.gameObject.AddComponent<Button>();
            button.SetOnClick(() =>
            {
                if (WikiLinkTable.TryGetValue(getName(), out var link))
                {
                    OpenLink(link);
                }
            });
        }

        button.enabled = hasLink;
        text.fontStyle = hasLink && UnderlineLinks ? FontStyles.Underline : FontStyles.Normal;
        text.raycastTarget = hasLink;
        if (lineSpacing.HasValue)
        {
            text.lineSpacing = hasLink ? lineSpacing.Value : 0f;
        }
    }

    private static bool firstTime = true;

    private static void OpenLink(string link)
    {
        var fullLink = Path.Combine(WikiUrl, link);

        if (EmbeddedBrowser && !ModHelper.IsEpic)
        {
            OpenEmbeddedLink(fullLink);
        }
        else
        {
            ProcessHelper.OpenURL(fullLink);
        }
    }

    private static void OpenEmbeddedLink(string fullLink)
    {
        var embeddedBrowser = ModHelper.GetMod("BloonsTD6 Mod Helper").MelonAssembly.Assembly
            .GetType("BTD_Mod_Helper.UI.BTD6.EmbeddedBrowser")!;
        var open = embeddedBrowser.GetMethod("OpenURL", BindingFlags.NonPublic | BindingFlags.Static)!;

        open.Invoke(null, [
            fullLink, new Action<SteamWebView>(view =>
            {
                view.EvaluateJavaScript(cleanWikiPageScript);
                if (firstTime)
                {
                    firstTime = false;
                    TaskScheduler.ScheduleTask(() => view.Exists()?.Reload(), ScheduleType.WaitForSeconds, 1);
                }
            })
        ]);
        
    }

    [HarmonyPatch(typeof(UpgradeScreen), nameof(UpgradeScreen.UpdateUi))]
    [HarmonyPriority(Priority.Last)]
    internal static class UpgradeScreen_UpdateUi
    {
        [HarmonyPostfix]
        private static void Postfix(UpgradeScreen __instance)
        {
            Setup(__instance.towerTitle, () => __instance.currTowerId);
            Setup(__instance.towerTitleParagon, () => __instance.currTowerId);

            Setup(__instance.selectedUpgrade.upgradeName, () => __instance.selectedDetails.upgrade.name, 20);

            foreach (var upgradeDetails in __instance.GetComponentsInChildren<UpgradeDetails>())
            {
                Setup(upgradeDetails.upgradeName, () => upgradeDetails.upgrade.name);
            }
        }
    }

    [HarmonyPatch(typeof(HeroUpgradeDetails), nameof(HeroUpgradeDetails.BindDetails))]
    internal static class HeroUpgradeDetails_BindDetails
    {
        [HarmonyPostfix]
        private static void Postfix(HeroUpgradeDetails __instance)
        {
            Setup(__instance.heroName, () => __instance.SelectedHeroId);
        }
    }

    [HarmonyPatch(typeof(HeroInGameScreen), nameof(HeroInGameScreen.Open))]
    internal static class HeroInGameScreen_Open
    {
        [HarmonyPostfix]
        private static void Postfix(HeroInGameScreen __instance)
        {
            Setup(__instance.heroName, () => __instance.heroId);
        }
    }

    [HarmonyPatch(typeof(PowersSelectScreen), nameof(PowersSelectScreen.Open))]
    internal static class PowersSelectScreen_Open
    {
        [HarmonyPostfix]
        private static void Postfix(PowersSelectScreen __instance)
        {
            foreach (var powerSelectButton in __instance.powerButtons)
            {
                Setup(powerSelectButton.powerNameText, () => powerSelectButton.powerModel.name, 20);
            }
        }
    }

#if DEBUG

    private static readonly ModSettingButton GenerateLinks = new(() => Task.Run(GenerateWikiLinks));
    
    private static async void GenerateWikiLinks()
    {
        var jobject = new JObject();

        var towers = Game.instance.model.towerSet.Where(model => IsBase(model.towerId)).ToArray();

        foreach (var tower in towers)
        {
            var name = LocalizationManager.Instance.GetTextEnglish(tower.towerId);

            var link = await GetWikiLink(name);

            if (link != null)
            {
                jobject[tower.towerId] = link;
                WikiLinkTable[tower.towerId] = link;
            }
        }

        var heroes = Game.instance.model.heroSet.Where(model => IsBase(model.towerId)).ToArray();

        foreach (var hero in heroes)
        {
            var name = LocalizationManager.Instance.GetTextEnglish(hero.towerId);

            var link = await GetWikiLink(name);

            if (link != null)
            {
                jobject[hero.towerId] = link;
                WikiLinkTable[hero.towerId] = link;
            }
        }

        var upgradesByTower = towers.ToDictionary(
            model => model.towerId,
            model => Game.instance.model.GetTowersWithBaseId(model.towerId)
                .SelectMany(towerModel => towerModel.appliedUpgrades)
                .Distinct()
        );

        foreach (var (tower, upgrades) in upgradesByTower)
        {
            foreach (var upgrade in upgrades)
            {
                var name = LocalizationManager.Instance.GetTextEnglish(upgrade);
                var towerName = LocalizationManager.Instance.GetTextEnglish(tower);
                var link = await GetWikiLink(name, towerName);

                if (link != null)
                {
                    jobject[upgrade] = link;
                    WikiLinkTable[upgrade] = link;
                }
            }
        }

        var powers = Game.instance.model.powers.Where(model => !model.isHidden).ToArray();

        foreach (var power in powers)
        {
            var name = LocalizationManager.Instance.GetTextEnglish(power.name);
            var link = await GetWikiLink(name);

            if (link != null)
            {
                jobject[power.name] = link;
                WikiLinkTable[power.name] = link;
                jobject["PowersInShop-" + power.name] = link;
                WikiLinkTable["PowersInShop-" + power.name] = link;
            }
        }

        var path = Path.Combine(ModHelper.ModSourcesDirectory, nameof(UsefulUtilities), "Resources", WikiLinksJSON);

        await File.WriteAllTextAsync(path, jobject.ToString(Formatting.Indented));

        ModHelper.Msg<UsefulUtilitiesMod>($"Wrote wiki links to {path}");
    }

    private static bool IsBase(string name) => !ModHelper.Mods.Any(mod => name.StartsWith(mod.IDPrefix));

    private static string NameToLink(string name) =>
        Regex.Replace(name.Replace(" ", "_"), @"(-[a-z])", m => m.ToString().ToUpper());

    internal static async Task<string?> GetWikiLink(string name, string towerName = "")
    {
        var normalLink = NameToLink(name);
        var btd6Link = normalLink + "_(BTD6)";
        var disambiguatedLink = normalLink + $"_({NameToLink(towerName)})";
        var disambiguatedBtd6Link = normalLink + $"_(BTD6_{NameToLink(towerName)})";

        foreach (var link in new[] { btd6Link, normalLink, disambiguatedBtd6Link, disambiguatedLink })
        {
            if (await TryLink(link)) return link;
        }

        ModHelper.Warning<UsefulUtilitiesMod>($"No link for {name}");

        return null;
    }

    internal static async Task<bool> TryLink(string link)
    {
        try
        {
            var response = await ModHelperHttp.Client.GetStringAsync(WikiUrl + link);

            return !response.Contains("There is currently no text in this page.") &&
                   !response.Contains("This article is a disambiguation page");
        }
        catch (Exception)
        {
            return false;
        }
    }

#endif
}