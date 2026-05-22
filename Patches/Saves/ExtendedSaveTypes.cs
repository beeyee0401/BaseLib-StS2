using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Saves;

[HarmonyPatch(typeof(MegaCritSerializerContext), "global::System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver.GetTypeInfo")]
public class ExtendedSaveTypes
{
    [HarmonyPostfix]
    static void GetExtraType(MegaCritSerializerContext __instance, Type type, JsonSerializerOptions options, ref JsonTypeInfo? __result)
    {
        if (__result != null) return;
        
        BaseLibMain.Logger.Info($"Type {type} missing for serialization, checking extended types");

        if (!ExtendedTypes.TryGetValue(type, out var typeInfoFunc)) return;

        __result = typeInfoFunc(__instance, options);
    }
    
    private static readonly Dictionary<Type, Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo>> ExtendedTypes = [];

    /// <summary>
    /// Returns true if the type has TypeInfo available in the serializer context.
    /// Type info can be added by calling the RegisterSaveType methods on this class.
    /// </summary>
    public static bool IsSaveTypeSupported(Type t)
    {
        return MegaCritSerializerContext.Default.GetTypeInfo(t) != null;
    }

    /// <summary>
    /// Returns true if BaseLib supports saving values on this type.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static bool IsSaveHolderSupported(Type t)
    {
        return t.IsAssignableTo(typeof(CardModel))
               || t.IsAssignableTo(typeof(RelicModel))
               || t.IsAssignableTo(typeof(PotionModel))
               || t.IsAssignableTo(typeof(Player))
               || t.IsAssignableTo(typeof(Reward));
    }
    
    /// <summary>
    /// Attempts to register a saved value. Returns false if the target type is unsupported.
    /// </summary>
    public static bool RegisterSavedValue<TargetType, T>(string id, Func<TargetType, T?> getter, Action<TargetType, T?> setter,
        Action<T, PacketWriter> serializer, Func<PacketReader, T> deserializer)
    {
        var targetType = typeof(TargetType);

        if (targetType.IsAssignableTo(typeof(CardModel)))
        {
            ExtendedSaveHandlers<CardModel, SerializableCard>.RegisterSave<T>(id,
                card =>
                {
                    if (card is not TargetType target) return default;
                    return getter(target);
                },
                (card, val) =>
                {
                    if (card is not TargetType target) return;
                    setter(target, val);
                }, serializer, deserializer);

            return true;
        }
        if (targetType.IsAssignableTo(typeof(RelicModel)))
        {
            ExtendedSaveHandlers<RelicModel, SerializableRelic>.RegisterSave<T>(id,
                relic =>
                {
                    if (relic is not TargetType target) return default;
                    return getter(target);
                },
                (relic, val) =>
                {
                    if (relic is not TargetType target) return;
                    setter(target, val);
                }, serializer, deserializer);

            return true;
        }
        if (targetType.IsAssignableTo(typeof(PotionModel)))
        {
            ExtendedSaveHandlers<PotionModel, SerializablePotion>.RegisterSave<T>(id,
                potion =>
                {
                    if (potion is not TargetType target) return default;
                    return getter(target);
                },
                (potion, val) =>
                {
                    if (potion is not TargetType target) return;
                    setter(target, val);
                }, serializer, deserializer);

            return true;
        }
        if (targetType.IsAssignableTo(typeof(Player)))
        {
            ExtendedSaveHandlers<Player, SerializablePlayer>.RegisterSave<T>(id,
                player =>
                {
                    if (player is not TargetType target) return default;
                    return getter(target);
                },
                (player, val) =>
                {
                    if (player is not TargetType target) return;
                    setter(target, val);
                }, serializer, deserializer);

            return true;
        }
        if (targetType.IsAssignableTo(typeof(Reward)))
        {
            ExtendedSaveHandlers<Reward, SerializableReward>.RegisterSave<T>(id,
                reward =>
                {
                    if (reward is not TargetType target) return default;
                    return getter(target);
                },
                (reward, val) =>
                {
                    if (reward is not TargetType target) return;
                    setter(target, val);
                }, serializer, deserializer);

            return true;
        }
        if (targetType.IsAssignableTo(typeof(IRunState)))
        {
            ExtendedSaveHandlers<IRunState, SerializableRun>.RegisterSave<T>(id,
                reward =>
                {
                    if (reward is not TargetType target) return default;
                    return getter(target);
                },
                (reward, val) =>
                {
                    if (reward is not TargetType target) return;
                    setter(target, val);
                }, serializer, deserializer);

            return true;
        }
        
        BaseLibMain.Logger.Warn($"Could not register saved value {id}; type {typeof(TargetType).Name} is not set up in ExtendedSaveTypes.RegisterSavedValue");
        return false;
    }
    
    /// <summary>
    /// Registers a type to allow it to be saved and loaded by the basegame serializer.
    /// You are recommended to use the other shortcut methods provided by this class.
    /// </summary>
    public static void RegisterAdditionalSaveType<T>(Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo> typeInfoFunc)
    {
        if (ExtendedTypes.ContainsKey(typeof(T)))
            return;
        
        ExtendedTypes[typeof(T)] = typeInfoFunc;
    }

    /// <summary>
    /// Registers a general object/class type to be saved/loaded.
    /// Use the PropertyFunc and FieldFunc methods to define all properties and fields that will be saved.
    /// </summary>
    public static void RegisterObjectSaveType<T>(params Func<JsonSerializerOptions, JsonPropertyInfo>[] dataFunctions) where T : notnull, new()
    {
        if (ExtendedTypes.ContainsKey(typeof(T))) return;
        
        RegisterAdditionalSaveType<T>((resolver, options) =>
        {
            var objectInfo = new JsonObjectInfoValues<T>()
            {
                ObjectCreator = () => new T(),
                ObjectWithParameterizedConstructorCreator = null,
                PropertyMetadataInitializer = _ => dataFunctions.Select(func => func(options)).ToArray(),
                ConstructorParameterMetadataInitializer = null,
                ConstructorAttributeProviderFactory = 
                    () => typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                        null, [], null)!,
                SerializeHandler = null
            };
            var jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
            jsonTypeInfo.NumberHandling = null;
            jsonTypeInfo.OriginatingResolver = resolver;
            return jsonTypeInfo;
        });
    }

    /// <summary>
    /// Registers a specific type of Dictionary for saving.
    /// </summary>
    public static void RegisterDictionarySaveType<TKey, TValue>() where TKey : notnull
    {
        if (ExtendedTypes.ContainsKey(typeof(Dictionary<TKey, TValue>))) return;
        
        RegisterAdditionalSaveType<Dictionary<TKey, TValue>>((resolver, options) =>
        {
            var collectionInfo = new JsonCollectionInfoValues<Dictionary<TKey, TValue>>
            {
                ObjectCreator = () => [],
                SerializeHandler = null
            };
            var jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<TKey, TValue>, TKey, TValue>(options, collectionInfo);
            jsonTypeInfo.NumberHandling = null;
            jsonTypeInfo.OriginatingResolver = resolver;
            return jsonTypeInfo;
        });
    }

    /// <summary>
    /// Registers a specific type of List for saving.
    /// </summary>
    public static void RegisterListSaveType<TValue>()
    {
        if (ExtendedTypes.ContainsKey(typeof(List<TValue>))) return;
        
        RegisterAdditionalSaveType<List<TValue>>((resolver, options) =>
        {
            if (!MegaCritSerializerContext.TryGetTypeInfoForRuntimeCustomConverter<List<TValue>>(options, out var jsonTypeInfo))
            {
                var collectionInfo = new JsonCollectionInfoValues<List<TValue>>()
                {
                    ObjectCreator = () => [],
                    SerializeHandler = null
                };
                jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<TValue>, TValue>(options, collectionInfo);
                jsonTypeInfo.NumberHandling = null;
            }
            jsonTypeInfo.OriginatingResolver = resolver;
            return jsonTypeInfo;
        });
    }

    public static Func<JsonSerializerOptions, JsonPropertyInfo> PropertyFunc<DeclaringType, PropType>(string propName)
    {
        var prop = typeof(DeclaringType).GetProperty(propName);
        if (prop == null) throw new ArgumentException($"Unable to find public property '{propName}' in type {typeof(DeclaringType).Name}");
        
        return options =>
        {
            var propertyInfoValues = new JsonPropertyInfoValues<PropType>()
            {
                IsProperty = true,
                IsPublic = true,
                IsVirtual = false,
                DeclaringType = typeof(DeclaringType),
                Converter = null,
                Getter = obj => (PropType?)prop.GetValue(obj),
                Setter = (obj, value) => prop.SetValue(obj, value),
                IgnoreCondition = null,
                HasJsonInclude = false,
                IsExtensionData = false,
                NumberHandling = null,
                PropertyName = propName,
                JsonPropertyName = null,
                AttributeProviderFactory = () => prop
            };
            var propertyInfo = JsonMetadataServices.CreatePropertyInfo(options, propertyInfoValues);
            propertyInfo.IsGetNullable = false;
            propertyInfo.IsSetNullable = false;
            return propertyInfo;
        };
    }
    public static Func<JsonSerializerOptions, JsonPropertyInfo> FieldFunc<DeclaringType, FieldType>(string fieldName)
    {
        var field = typeof(DeclaringType).GetField(fieldName);
        if (field == null) throw new ArgumentException($"Unable to find public field '{fieldName}' in type {typeof(DeclaringType).Name}");
        
        return options =>
        {
            var propertyInfoValues = new JsonPropertyInfoValues<FieldType>()
            {
                IsProperty = false,
                IsPublic = true,
                IsVirtual = false,
                DeclaringType = typeof(DeclaringType),
                Converter = null,
                Getter = obj => (FieldType?)field.GetValue(obj),
                Setter = (obj, value) => field.SetValue(obj, value),
                IgnoreCondition = null,
                HasJsonInclude = false,
                IsExtensionData = false,
                NumberHandling = null,
                PropertyName = fieldName,
                JsonPropertyName = null,
                AttributeProviderFactory = () => field
            };
            var propertyInfo = JsonMetadataServices.CreatePropertyInfo(options, propertyInfoValues);
            propertyInfo.IsGetNullable = false;
            propertyInfo.IsSetNullable = false;
            return propertyInfo;
        };
    }
}