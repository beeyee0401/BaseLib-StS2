/* CustomLocTablePatches.cs
 * Authors: Pikcube, lamali
 * Date Last Modified: 2026-05-18
 * Description: Patches the LocManager to add any loc tables registered in in CustomLocTableManager
 *
 * Usages:
 * CustomLocManager.Register("mytable.json");
 * LocManager.Instance.Register("mytable.json");
 */

using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using FileAccess = Godot.FileAccess;

namespace BaseLib.Patches.Localization;

internal static class CustomLocTablePatches
{
    [HarmonyPatch(typeof(LocManager), "ListLocalizationFiles")]
    internal static class ListLocalizationFilesPatch
    {
        //Append any additional loc tbales to the list of localization files.
        public static IEnumerable<string> Postfix(IEnumerable<string> __result)
        {
            return CustomLocTableManager.GetCustomLocTables(__result);
        }
    }

    [HarmonyPatch(typeof(LocManager), "LoadTable")]
    internal static class LoadTablePatch
    {
        //Prevents the game from throwing an error when trying to load a localization file not present in the base game by force returning an empty dictionary if the file isn't found.
        public static bool Prefix(string path, ref Dictionary<string, string> __result)
        {
            if (FileAccess.FileExists(path))
            {
                return true;
            }

            __result = [];
            return false;
        }
    }
}