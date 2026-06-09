using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Hooks;

/// <summary>
/// Interface for models that should know when a card is downgraded
/// </summary>
public interface IAfterCardDowngradedSubscriber
{
    /// <summary>
    /// Fires after a card is downgraded
    /// </summary>
    void AfterCardDowngraded(CardModel card);

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.DowngradeInternal))]
    private static class TriggerPatch
    {
        private static void Postfix(CardModel __instance)
        {
            var combatState = __instance.CombatState;
            var runState = __instance.Owner?.RunState ?? (combatState == null ? NullRunState.Instance : new CombatStateWrapper(combatState).RunState);
            foreach (var item in BetaMainCompatibility.RunState.IterateHookListeners.Invoke<IEnumerable<AbstractModel>>(runState, combatState) ?? [])
            {
                if (item is IAfterCardDowngradedSubscriber subscriber)
                    subscriber.AfterCardDowngraded(__instance);
            }
        }
    }
}