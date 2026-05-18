/* CustomLocTableManager.cs
 * Authors: Pikcube
 * Date Last Modified: 2026-05-18
 * Description: Adds a public facing static class for registering custom loc tables
 *
 * Usages:
 * CustomLocManager.Register("mytable.json");
 * LocManager.Instance.Register("mytable.json");
 */

using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Utils;

/// <summary>
/// Static class for tracking Loc Tables added by other mods
/// </summary>
public static class CustomLocTableManager
{
    private static readonly HashSet<string> LocTables = [];
    /// <summary>
    /// Append any registered LocTables to the list of custom loc tables.
    /// </summary>
    /// <param name="original">The original list of Localization Files</param>
    /// <returns>The original list with this.LocTables appended to the end.</returns>
    internal static IEnumerable<string> GetCustomLocTables(IEnumerable<string> original)
    {
        return [.. original, .. LocTables];
    }

    /// <summary>
    /// Add a custom loc table to the game.
    /// </summary>
    /// <param name="name">The name of the json file to register.</param>
    public static void Register(string name)
    {
        if (!name.EndsWith(".json"))
        {
            name += ".json";
        }

        LocTables.Add(name);
    }

    /// <summary>
    /// Adds a custom loc table to the game using BaseLib.Utils.CustomLocTableManager.<br/>
    /// Equivalent to CustomLocTableManager.Register(name);
    /// </summary>
    /// <param name="locManager">The current loc manager.</param>
    /// <param name="name">The name of the json file to register.</param>
    public static void RegisterCustomLocTable(this LocManager locManager, string name) => Register(name);     //This is just an alais I added to try and make this more discoverable for people using intellisense.
}