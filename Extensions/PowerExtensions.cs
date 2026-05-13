using BaseLib.Abstracts;
using Baselib.Patches.Utils;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Extensions;

/// <summary>
/// Extension methods for <see cref="IHasSecondAmount"/> powers.
/// Connects them to their <see cref="NPower"/> display node,
/// allowing powers to trigger a label refresh without access to the node directly.
/// </summary>
public static class PowerExtensions
{
 
    /// <summary>
    /// Triggers a UI refresh of the second amount label for this power.
    /// Call this whenever the value returned by <see cref="IHasSecondAmount.GetSecondAmount"/> changes.
    /// </summary>
    /// <remarks>
    /// Typical usage inside a power:
    /// <code>
    /// this.InvokeSecondAmountChanged();
    /// </code>
    /// </remarks>
    public static void InvokeSecondAmountChanged(this IHasSecondAmount power)
    {
        if (SecondAmountRegistry.RefreshActions.TryGetValue(power, out var refresh))
            refresh();
    }
}
