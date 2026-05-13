using BaseLib.Extensions;

namespace BaseLib.Abstracts;

/// <summary>
/// Exposes a secondary display amount alongside the primary power amount.
/// Implement this interface on any <c>PowerModel</c> that needs to show a
/// second numeric or textual value in the UI (e.g. a counter or cooldown).
/// </summary>
/// <remarks>
/// When the value changes, call <see cref="PowerExtensions.InvokeSecondAmountChanged"/>
/// to trigger a UI refresh.
/// </remarks>
public interface IHasSecondAmount
{
    /// <summary>
    /// Gets the secondary amount as a formatted string for UI display.
    /// </summary>
    /// <returns>
    /// A string representing the secondary value.
    /// </returns>
    string GetSecondAmount();
}