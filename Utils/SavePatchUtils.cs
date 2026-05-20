using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Utils;

//Utilities to help with adding properties to mod saves.
public class SavePatchUtils
{
    private static readonly HashSet<Type> _supportedTypes =
    [
        typeof(int),
        typeof(int[]),
        typeof(ModelId),
        typeof(bool),
        typeof(string),
        typeof(SerializableCard),
        typeof(List<SerializableCard>)
    ];
    public static bool IsTypeSupported(Type type)
    {
        return _supportedTypes.Contains(type) || type.IsEnum;
    }
    
    /// <summary>
    /// Quickly sets up the properties of a JsonPropertyInfoValues object for an actual property.
    /// </summary>
    /// <typeparam name="ModifyingType">The type whose serialization is being modified, from which values must be obtained.</typeparam>
    /// <typeparam name="DeclaringType">The type in which the extra property is defined.</typeparam>
    /// <typeparam name="PropType">The property's type.</typeparam>
    /// <returns></returns>
    public static JsonPropertyInfoValues<PropType> QuickProps<ModifyingType, DeclaringType, PropType>(string propName,
        Func<ModifyingType, PropType?> getter, Action<ModifyingType, PropType?> setter)
    {
        return new JsonPropertyInfoValues<PropType>
        {
            IsProperty = true,
            IsPublic = true,
            IsVirtual = false,
            DeclaringType = typeof(ModifyingType),
            Converter = null,
            Getter = (obj) => getter((ModifyingType) obj),
            Setter = (obj, val) => setter((ModifyingType) obj, val),
            IgnoreCondition = null,
            HasJsonInclude = false,
            IsExtensionData = false,
            NumberHandling = null,
            PropertyName = propName,
            JsonPropertyName = propName,
            AttributeProviderFactory = () => typeof(DeclaringType)
                .GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(PropType), [], null)!
        };
    }
    
    /// <summary>
    /// Quickly sets up the properties of a JsonPropertyInfoValues object with a custom name for a fake property.
    /// </summary>
    /// <typeparam name="ModifyingType">The type whose serialization is being modified, from which values must be obtained.</typeparam>
    /// <typeparam name="DeclaringType">The type in which the extra property is defined.</typeparam>
    /// <typeparam name="PropType">The property's type.</typeparam>
    /// <returns></returns>
    public static JsonPropertyInfoValues<PropType> QuickProps<ModifyingType, PropType>(string propName,
        Func<ModifyingType, PropType?> getter, Action<ModifyingType, PropType?> setter)
    {
        return new JsonPropertyInfoValues<PropType>
        {
            IsProperty = true,
            IsPublic = true,
            IsVirtual = false,
            DeclaringType = typeof(ModifyingType),
            Converter = null,
            Getter = (obj) => getter((ModifyingType) obj),
            Setter = (obj, val) => setter((ModifyingType) obj, val),
            IgnoreCondition = null,
            HasJsonInclude = false,
            IsExtensionData = false,
            NumberHandling = null,
            PropertyName = propName,
            JsonPropertyName = propName
        };
    }
}

public sealed record ExtendedSaveInfo<DataSourceType, DataHolderType>(string Id, 
    Action<DataSourceType, DataHolderType> Getter, Action<DataSourceType, DataHolderType> Setter,
    Action<DataHolderType, PacketWriter> Serializer, Action<DataHolderType, PacketReader> Deserializer)
    : IComparable<ExtendedSaveInfo<DataSourceType, DataHolderType>>
{
    /// <inheritdoc />
    public int CompareTo(ExtendedSaveInfo<DataSourceType, DataHolderType>? other)
    {
        return string.Compare(Id, other?.Id, StringComparison.Ordinal);
    }
}