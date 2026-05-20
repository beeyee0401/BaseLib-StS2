using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Extensions;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Saves;

public class ExtendedSerializableCard
{
    public static readonly NotNullSpireField<SerializableCard, ExtendedSerializableCard> ExtendedData = new(() => new());
    public static readonly List<ExtendedSaveInfo<CardModel, ExtendedSerializableCard>> RegisteredSaves = [];
    private static readonly Dictionary<Type, Func<JsonSerializerOptions, JsonPropertyInfo>> SaveValueTypes = [];
    private static bool _initializedSaveProps = false;
    
    /// <summary>
    /// Registers a value to be saved attached to a card and be copied in SerializableCard's Serialize/Deserialize methods.
    /// </summary>
    /// <param name="id">The ID of the saved value. Should ideally be a unique string involving the mod's ID.</param>
    /// <param name="getter">Gets the value to save given a card instance.</param>
    /// <param name="setter">Given a saved value, attaches it to a card instance.</param>
    /// <typeparam name="T">A type that implements IPacketSerializable and has a no-parameter constructor.</typeparam>
    public static void RegisterCardSave<T>(string id, Func<CardModel, T> getter, Action<CardModel, T> setter)
        where T : IPacketSerializable, new()
    {
        RegisterCardSave(id, getter, setter, 
            (val, writer) => val.Serialize(writer),
            (reader) =>
            {
                var val = new T();
                val.Deserialize(reader);
                return val;
            });
    }
    
    /// <summary>
    /// Registers a value to be saved attached to a card and be copied in SerializableCard's Serialize/Deserialize methods.
    /// </summary>
    /// <param name="id">The ID of the saved value. Should ideally be a unique string involving the mod's ID.</param>
    /// <param name="getter">Gets the value to save given a card instance.</param>
    /// <param name="setter">Given a saved value, attaches it to a card instance.</param>
    /// <param name="serializer">Writes the saved value with a PacketWriter.</param>
    /// <param name="deserializer">Retrives the saved value from a PacketReader.</param>
    /// <typeparam name="T">The saved type.</typeparam>
    public static void RegisterCardSave<T>(string id, Func<CardModel, T> getter, Action<CardModel, T> setter,
        Action<T, PacketWriter> serializer, Func<PacketReader, T> deserializer)
    {
        ExtendedSaveTypes.RegisterDictionarySaveType<string, T>();
        
        if (!SaveValueTypes.ContainsKey(typeof(T))) 
        {
            if (_initializedSaveProps)
            {
                BaseLibMain.Logger.Warn($"Saved types for cards have already been registered; registered save values of type {typeof(T).Name} will not be saved.");
            }

            SaveValueTypes.Add(typeof(T),
                options => JsonMetadataServices.CreatePropertyInfo(options,
                    SavePatchUtils.QuickProps<SerializableCard, Dictionary<string, T>>(
                        $"save_dict_{MakeTypeName(typeof(T))}",
                        obj => ExtendedData[obj].DictForType<T>(),
                        (obj, value) =>
                        {
                            if (value == null) return;
                            ExtendedData[obj].Dictionaries[typeof(T)] = value;
                        })
                    )
                );
        }

            
        RegisteredSaves.InsertSorted(new (id,
            (model, data) =>
            {
                if (!data.DictForType<T>().TryAdd(id, getter(model)))
                {
                    BaseLibMain.Logger.Error($"DUPLICATE CARD SAVE KEY: [{typeof(T).Name}] {id}");
                }
            },
            (model, data) =>
            {
                if (data.DictForType<T>().TryGetValue(id, out var value))
                {
                    setter(model, value);
                }
            },
            (data, writer) =>
            {
                var val = data.DictForType<T>().GetValueOrDefault(id);
                if (val == null)
                {
                    writer.WriteBool(false);
                }
                else
                {
                    writer.WriteBool(true);
                    serializer(val, writer);
                }
            },
            (data, reader) =>
            {
                bool exists = reader.ReadBool();
                if (exists)
                {
                    data.DictForType<T>()[id] = deserializer(reader);
                }
            }
        ));
    }

    private static string MakeTypeName(Type t)
    {
        var name = GetShortName(t);
        if (t.IsGenericType)
        {
            return $"{name}[{t.GenericTypeArguments.Join(MakeTypeName, ",")}]";
        }
        return $"{name}";
    }

    private static string GetShortName(Type t)
    {
        if (t.IsAssignableTo(typeof(IList))) return "List";
        if (t.IsAssignableTo(typeof(IDictionary))) return "Dictionary";
        return t.FullName == null || t.FullName.StartsWith("System") ? t.Name : t.FullName;
    }

    /// <summary>
    /// Creates the JsonPropertyInfo 
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IEnumerable<JsonPropertyInfo> CreateExtendedProperties(JsonSerializerOptions options)
    {
        BaseLibMain.Logger.Info("Adding custom save data to SerializableCard.");
        //All save data types must be initialized by this point. Any added later will not be saved/loaded.
        _initializedSaveProps = true;
        foreach (var saveType in SaveValueTypes)
        {
            yield return saveType.Value(options);
        }
    }

    /// <summary>
    /// Loads data from a loaded/deserialized serializable card to a new card.
    /// </summary>
    public static void Load(SerializableCard dataSource, CardModel card)
    {
        var data = ExtendedData[dataSource];
        foreach (var save in RegisteredSaves)
        {
            save.Setter.Invoke(card, data);
        }
    }

    /// <summary>
    /// Dictionaries for each type of data that will be saved.
    /// </summary>
    public readonly Dictionary<Type, IDictionary> Dictionaries = [];
    /// <summary>
    /// Gets or creates the dictionary for a specific type of data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Dictionary<string, T> DictForType<T>() => 
        Dictionaries.TryGetValue(typeof(T), out var dict) ? (Dictionary<string, T>)dict : 
        Dictionaries.TryAdd(typeof(T), dict = new Dictionary<string, T>()) ? (Dictionary<string, T>)dict : throw new Exception("Failed to add missing type to dictionary");

    /// <summary>
    /// Creates data from a card.
    /// Used when serializing a card.
    /// </summary>
    /// <param name="card"></param>
    public ExtendedSerializableCard(CardModel card)
    {
        foreach (var save in RegisteredSaves)
        {
            save.Getter.Invoke(card, this);
        }
    }

    /// <summary>
    /// Creates an empty data holder.
    /// Used for loading from json.
    /// </summary>
    public ExtendedSerializableCard()
    {
        
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ToSerializable))]
static class PrepExtendedCardData
{
    [HarmonyPostfix]
    static void ExtendedDataForCard(CardModel __instance, SerializableCard __result)
    {
        var data = new ExtendedSerializableCard(__instance);
        ExtendedSerializableCard.ExtendedData[__result] = data;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
static class LoadExtendedCardData
{
    [HarmonyTranspiler]
    static List<CodeInstruction> InsertLoad(IEnumerable<CodeInstruction> code)
    {
        return new InstructionPatcher(code)
                .Match(new InstructionMatcher()
                    .call(typeof(SavedProperties), nameof(SavedProperties.Fill)))
                .TakeLabels(out var labels)
                .Insert([
                    CodeInstruction.LoadArgument(0).WithLabels(labels), //SerializableCard
                    CodeInstruction.LoadLocal(0), //Creating card
                    CodeInstruction.Call(typeof(ExtendedSerializableCard), nameof(ExtendedSerializableCard.Load))
                ])
            ;
    }
}

[HarmonyPatch(typeof(SerializableCard), nameof(SerializableCard.Serialize))]
static class SerializeExtendedCardData
{
    [HarmonyPrefix] //Prefix instead of postfix due to inconsistent written length of SerializableCard
    //Difference between basegame is not an issue as this serialization is only used for net communication, not saves
    static void WriteExtended(SerializableCard __instance, PacketWriter writer)
    {
        var extendedData = ExtendedSerializableCard.ExtendedData[__instance];
        foreach (var saveValue in ExtendedSerializableCard.RegisteredSaves)
        {
            saveValue.Serializer(extendedData, writer);
        }
    }
}

[HarmonyPatch(typeof(SerializableCard), nameof(SerializableCard.Deserialize))]
static class DeserializeExtendedCardData
{
    [HarmonyPrefix] //Prefix instead of postfix due to inconsistent written length of SerializableCard
    static void ReadExtended(SerializableCard __instance, PacketReader reader)
    {
        var extendedData = ExtendedSerializableCard.ExtendedData[__instance];
        foreach (var saveValue in ExtendedSerializableCard.RegisteredSaves)
        {
            saveValue.Deserializer(extendedData, reader);
        }
    }
}

[HarmonyPatch(typeof(MegaCritSerializerContext), nameof(MegaCritSerializerContext.SerializableCardPropInit))]
static class AddExternalCardProperties
{
    [HarmonyPostfix]
    static void AdjustPropArray(JsonSerializerOptions options, ref JsonPropertyInfo[] __result)
    {
        int oldCount = __result.Length;
        __result = [..__result,
            ..ExtendedSerializableCard.CreateExtendedProperties(options)
        ];
        BaseLibMain.Logger.Info($"Added {__result.Length - oldCount} new properties to SerializableCard");
    }
}