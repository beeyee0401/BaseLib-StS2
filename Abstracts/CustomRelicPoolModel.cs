using MegaCrit.Sts2.Core.Models;
using BaseLib.Patches.Content;
using BaseLib.Patches.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

public abstract class CustomRelicPoolModel : RelicPoolModel, ICustomModel, ICustomEnergyIconPool
{
    public CustomRelicPoolModel()
    {
        if (IsShared) ModelDbSharedRelicPoolsPatch.Register(this);
    }

    protected override IEnumerable<RelicModel> GenerateAllRelics() => []; //Content added through ModHelper.ConcatModelsFromMods

    /// <summary>
    /// You shouldn't need this (just use SharedRelicPool), but it is allowed.
    /// </summary>
    public virtual bool IsShared => false;

    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;

    /// <summary>
    /// Override to true if all relics in this pool should automatically be marked as seen in the compendium
    /// </summary>
    public virtual bool SeenByDefault => false;
}

[HarmonyPatch(typeof(NRelicCollection), "LoadRelics")]
static class CustomRelicPoolMarkAsSeenPatch
{
    [HarmonyPrefix]
    public static void MarkAllAsSeen()
    {
        foreach (var relicPool in ModelDb.AllRelicPools)
            if (relicPool is CustomRelicPoolModel customRelicPool && customRelicPool.SeenByDefault)
                foreach (var relic in relicPool.AllRelics) SaveManager.Instance.MarkRelicAsSeen(relic);
    }
}
