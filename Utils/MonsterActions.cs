using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public static class MonsterActions
{
    public static AttackCommand Attack(MonsterModel monster, int baseDmg, int hitCount = 1)
    {
        var result = new AttackCommand(baseDmg)
            .FromMonster(monster);
        if (hitCount != 1)
            result.WithHitCount(hitCount);
        return result;
    }
    
    /// <summary>
    /// Applies the power specified as the generic parameter to the calling monster.
    /// </summary>
    public static async Task<T?> ApplySelf<T>(MonsterModel monster, int amount, PlayerChoiceContext? context = null, bool silent = false) where T : PowerModel
    {
        return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T?>, T>
            (null, context ?? new ThrowingPlayerChoiceContext(), monster.Creature, amount, monster.Creature, null, silent)!;
    }
    
    /// <summary>
    /// Applies the power specified as the generic parameter to the provided targets.
    /// </summary>
    public static async Task<T?> Apply<T>(MonsterModel monster, int amount, IEnumerable<Creature> targets, PlayerChoiceContext? context = null, bool silent = false) where T : PowerModel
    {
        return await BetaMainCompatibility.PowerCmd_.ApplyMulti.InvokeGeneric<Task<T?>, T>
            (null, context ?? new ThrowingPlayerChoiceContext(), targets, amount, monster.Creature, null, silent)!;
    }
}