using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using Il2CppAssets.Scripts.Unity.Bridge;
namespace UsefulUtilities.Utilities.InGameCharts.StatTypes;

public abstract class StatType : NamedModContent
{
    private static readonly Dictionary<int, StatType> Cache = new();

    public sealed override void Register()
    {
        Cache[Order] = this;
    }

    protected abstract override int Order { get; }

    public abstract string Icon { get; }

    public static implicit operator string(StatType statType) => statType.Name;

    public static implicit operator StatType(int i) => Of(i);

    public static StatType Of(int i) => Cache[i];

    public abstract double Calculate(TowerToSimulation tower);

    public virtual double Calculate(IEnumerable<TowerToSimulation> towers) => towers.Sum(Calculate);
}