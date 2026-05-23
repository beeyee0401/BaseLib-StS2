using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
static class CustomAnimationPatch
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))] //Starts playing death animation, returns time
    [HarmonyPostfix]
    static void AdjustTime(NCreature __instance, ref float __result)
    {
        if (__instance.Entity.Player?.Character is CustomCharacterModel character)
        {
            if (CustomAnimation.HasCustomAnimation(__instance))
            {
                __result = Math.Min(character.DeathAnimTime, 5f);
            }
        }
    }

    //Called if creature has no spine animation or has no revive animation defined
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.AnimTempRevive))]
    [HarmonyPrefix]
    static bool UseCustomReviveAnim(NCreature __instance)
    {
        if (__instance.HasSpineAnimation) return true;
        
        return !CustomAnimation.PlayCustomAnimation(__instance, "revive", "Revive");
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
    [HarmonyPrefix]
    static bool SendTriggerToOtherAnimators(NCreature __instance, string trigger)
    {
        if (__instance.HasSpineAnimation) return true;
        
        BaseLibMain.Logger.Debug($"SetAnimationTrigger called for {trigger} on creature without spine animation");
        
        var animName = trigger switch
        {
            CreatureAnimator.idleTrigger => "idle",
            CreatureAnimator.attackTrigger => "attack",
            CreatureAnimator.castTrigger => "cast",
            CreatureAnimator.hitTrigger => "hurt",
            CreatureAnimator.deathTrigger => "die",
            _ => trigger.ToLowerInvariant()
        };
        
        return !CustomAnimation.PlayCustomAnimation(__instance, animName, trigger, trigger.ToLowerInvariant());
    }
}