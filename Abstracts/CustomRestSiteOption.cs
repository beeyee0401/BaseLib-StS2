using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace BaseLib.Abstracts;

/// <summary>
/// class for modded rest site options that support overriding the icon path.
/// </summary>
/// <param name="owner">The player that owns this rest site option.</param>
public abstract class CustomRestSiteOption(Player owner) : RestSiteOption(owner)
{
    /// <summary>
    /// Custom icon path.
    /// Return <see langword="null"/> to use the default icon behavior.
    /// </summary>
    public virtual string? CustomIconPath => null;
}

[HarmonyPatch(typeof(RestSiteOption), "IconPath", MethodType.Getter)]
internal class CustomRestSiteOptionIconPath
{
    [HarmonyPrefix]
    private static bool Custom(RestSiteOption __instance, ref string __result)
    {
        if (__instance is not CustomRestSiteOption { CustomIconPath: { } path })
            return true;
        __result = path;
        return false;
    }
}