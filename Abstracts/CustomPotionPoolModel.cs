using MegaCrit.Sts2.Core.Models;
using BaseLib.Patches.Content;
using BaseLib.Patches.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

public abstract class CustomPotionPoolModel : PotionPoolModel, ICustomModel, ICustomEnergyIconPool
{
    public CustomPotionPoolModel()
    {
        if (IsShared) ModelDbSharedPotionPoolsPatch.Register(this);
    }

    protected override IEnumerable<PotionModel> GenerateAllPotions() => []; //Content added through ModHelper.ConcatModelsFromMods

    /// <summary>
    /// You shouldn't need this (just use SharedRelicPool), but it is allowed.
    /// </summary>
    public virtual bool IsShared => false;

    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;

    /// <summary>
    /// Override to true if all potions in this pool should automatically be marked as seen in the compendium
    /// </summary>
    public virtual bool SeenByDefault => false;
}

[HarmonyPatch(typeof(NPotionLab), "LoadPotions")]
static class CustomPotionPoolMarkAsSeenPatch
{
    [HarmonyPrefix]
    public static void MarkAllAsSeen()
    {
        foreach (var potionPool in ModelDb.AllPotionPools)
            if (potionPool is CustomPotionPoolModel customPotionPool && customPotionPool.SeenByDefault)
                foreach (var potion in potionPool.AllPotions) SaveManager.Instance.MarkPotionAsSeen(potion);
    }
}
