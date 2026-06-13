using BaseLib.Extensions;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

class AddVarsToCard
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile),
        typeof(PileType), typeof(CardModel.DescriptionPreviewType), typeof(Creature))]
    static class AddVarsToCardDescription
    {
        [HarmonyTranspiler]
        static List<CodeInstruction> Patch(IEnumerable<CodeInstruction> code)
        {
            return new InstructionPatcher(code)
                .Match(new CallMatcher(typeof(CardModel).DeclaredMethod("AddExtraArgsToDescription")))
                .Insert([
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadLocal(0),
                    CodeInstruction.Call(typeof(AddVarsToCardDescription), nameof(AddVarsToCardDescription.AddExtraVars))
                ]);
        }

        static void AddExtraVars(CardModel card, LocString description)
        {
            foreach (var mod in card.GetModifiers())
            {
                mod.AddToDescription(description);
            }
        }
    }
}