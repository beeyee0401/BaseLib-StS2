using System.Reflection.Emit;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class LoadStateSection : IAsyncMethodSection
{
    /// <summary>
    /// Reads a LoadStateSection using an enumerator that is already ready to read.
    /// </summary>
    public static LoadStateSection Read(AsyncMethodContext context, IEnumerator<CodeInstruction> codeEnumerator)
    {
        var loadsFoundStateField = false;
        var loadsStateFromDict = false;
        int stateKeyLocalIndex = -1;
        int stringDictLocalIndex = -1;
        
        List<CodeInstruction> loadStateSection = [];
        do
        {
            var instruction = codeEnumerator.Current;
            if (instruction.HasBlock(ExceptionBlockType.BeginExceptionBlock))
            {
                break;
            }

            loadStateSection.Add(instruction);
            if (instruction.LoadsField(context.StateField))
            {
                loadsFoundStateField = true;
            }

            if (instruction.Calls(AsyncMethodCall.LoadStateFromDictMethod))
            {
                loadsStateFromDict = true;
            }
        }
        while (codeEnumerator.MoveNext());

        if (!loadsFoundStateField)
        {
            throw new ArgumentException(
                $"MoveNext does not load found state field {context.StateField}; failed to set up AsyncMethodCall properly");
        }

        if (!loadsStateFromDict)
        {
            BaseLibMain.Logger.Debug("Setting up external state");
            var stateKeyLocal = context.Generator.DeclareLocal(typeof(int));
            var stringDictLocal = context.Generator.DeclareLocal(typeof(Dictionary<string, object>));
            
            stateKeyLocalIndex = stateKeyLocal.LocalIndex;
            stringDictLocalIndex = stringDictLocal.LocalIndex;
            
            loadStateSection =
            [
                CodeInstruction.LoadArgument(0), //One for ldfld, one for stfld
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Ldfld, context.StateField),
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.StoreLocal(stateKeyLocalIndex),
                new CodeInstruction(OpCodes.Call, AsyncMethodCall.LoadStateFromDictMethod),
                new CodeInstruction(OpCodes.Stfld, context.StateField),
                CodeInstruction.LoadLocal(stateKeyLocalIndex),
                new CodeInstruction(OpCodes.Call, AsyncMethodCall.LoadDictionaryForStateMethod),
                CodeInstruction.StoreLocal(stringDictLocalIndex),
                ..loadStateSection
            ];
        }
        else
        {
            BaseLibMain.Logger.Debug("Checking for external state loading");

            new InstructionPatcher(loadStateSection)
                .TryMatch(new InstructionMatcher()
                    .ldfld(context.StateField)
                    .dup()
                    .stloc_any()
                )
                ?.Step(-1)
                .GetIndexOperand(out stateKeyLocalIndex)
                .TryMatch(new InstructionMatcher()
                    .call(AsyncMethodCall.LoadDictionaryForStateMethod)
                    .stloc_any()
                )
                ?.Step(-1)
                .GetIndexOperand(out stringDictLocalIndex);
        }
        
        if (stateKeyLocalIndex == -1) //Loads state from dict but did not find local in which it is stored
        {
            throw new ArgumentException(
                "Failed to find local used to hold temporary state key.");
        }
        if (stringDictLocalIndex == -1)
        {
            throw new ArgumentException(
                "Failed to find local used to hold extra saved values.");
        }

        return new LoadStateSection
        {
            Code = loadStateSection,
            AddStateLoading = !loadsStateFromDict,
            StateKeyLocal = stateKeyLocalIndex,
            StringDictLocal = stringDictLocalIndex
        };
    }

    public required List<CodeInstruction> Code { get; init; }
    public required bool AddStateLoading { get; init; }
    public required int StringDictLocal { get; init; }
    public required int StateKeyLocal { get; init; }
    
    public IEnumerable<StateInfo> AllStates => [];
}