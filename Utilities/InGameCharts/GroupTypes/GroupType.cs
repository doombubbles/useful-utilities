using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using UnityEngine;
namespace UsefulUtilities.Utilities.InGameCharts.GroupTypes;

public abstract class GroupType : NamedModContent
{
    private static readonly Dictionary<int, GroupType> Cache = new();

    public sealed override void Register()
    {
        Cache[Order] = this;
    }

    protected abstract override int Order { get; }

    public static implicit operator string(GroupType statType) => statType.Name;

    public static implicit operator GroupType(int i) => Of(i);

    public static GroupType Of(int i) => Cache[i];

    public abstract string GroupId(TowerToSimulation tower);

    public abstract BarInfo BarInfo(TowerToSimulation tower, bool hideMonkeyNames);

    public static Color ColorForTower(TowerToSimulation tower) =>
        ColorForTowerSet(tower.IsParagon ? TowerSet.Paragon : tower.TowerSet());

    private static readonly Dictionary<string, string> IconCache = new();

    public static string GetBaseIcon(TowerToSimulation tower) =>
        IconCache.TryGetValue(tower.Def.baseId, out var icon)
            ? icon
            : IconCache[tower.Def.baseId] = Game.instance.model.GetTowerWithName(tower.Def.baseId).icon.AssetGUID;

    public static Color ColorForTowerSet(TowerSet towerSet) => towerSet switch
    {
        TowerSet.Primary => new Color(89 / 255f, 154 / 255f, 189 / 255f),
        TowerSet.Military => new Color(105 / 255f, 186 / 255f, 91 / 255f),
        TowerSet.Magic => new Color(137 / 255f, 96 / 255f, 210 / 255f),
        TowerSet.Support => new Color(219 / 255f, 148 / 255f, 99 / 255f),
        TowerSet.Hero => new Color(255 / 255f, 197 / 255f, 0 / 255f),
        TowerSet.Paragon => new Color(68 / 255f, 36 / 255f, 144 / 255f),
        _ => Color.white
    };


    public static string GetTowerName(TowerToSimulation t, bool hideMonkeyNames = true)
    {
        if (t.hero != null)
        {
            return t.towerSkinName;
        }

        if (!hideMonkeyNames &&
            Game.Player.Data.HasPurchasedNamedMonkeys() &&
            InGame.Bridge.GetNamedMonkeyName(t.owner, t.namedMonkeyKey) is { } name)
        {
            return name;
        }

        if (t.IsParagon)
        {
            return (t.Def.baseId + " Paragon").Localize();
        }

        var towerName = t.Def.baseId.Localize();

        if (t.Def.tiers != null && t.Def.tiers.Sum() > 0)
        {
            towerName = t.Def.tiers.Join(delimiter: "/") + towerName;
        }

        return towerName;
    }
}