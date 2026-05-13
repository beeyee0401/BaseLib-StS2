using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Non-generic base class of AncientOption used to keep options of various generic types in a collection.
/// </summary>
/// <param name="weight"></param>
public abstract class AncientOption(int weight) : IWeighted
{
    /// <inheritdoc/>
    public int Weight { get; } = weight;
    
    /// <summary>
    /// For special options like Orobas SeaGlass with multiple variants.
    /// </summary>
    public abstract IEnumerable<RelicModel> AllVariants { get; }
    /// <summary>
    /// Returns the mutable resulting option for an ancient event during a run.
    /// </summary>
    public abstract RelicModel ModelForOption { get; }
    
    /// <summary>
    /// Generates a basic ancient option for the given model without support for prep or custom weighting.
    /// </summary>
    public static explicit operator AncientOption(RelicModel model) => new BasicAncientOption(model, 1);
    
    private class BasicAncientOption(RelicModel model, int weight) : AncientOption(weight)
    {
        public override IEnumerable<RelicModel> AllVariants { get; } = [ model.ToMutable() ];
        public override RelicModel ModelForOption => model.ToMutable();
    }
}

/// <summary>
/// Generic class for an ancient option for a specific relic that supports custom weight and relics that need preparation
/// code for variations.
/// </summary>
public class AncientOption<T>(int weight) : AncientOption(weight) where T : RelicModel
{
    /// <summary>
    /// Set this if relic needs to set up data based on current run state, eg. Sea Glass choosing a random other character.
    /// Do not utilize the relic's owner field, it is not set until actually obtained.
    /// When defining this in an ancient use `Owner` directly to reference the ancient event's owner.
    /// This will be set up per-event instance.
    /// </summary>
    public Func<T, RelicModel>? ModelPrep { get; init; }
    
    /// <summary>
    /// Generate all possible variants of the relic. See Orobas.SeaGlassOptions for an example of this.
    /// Receives the canonical model of the relic. Should generally be static (not reference any fields of the ancient).
    /// </summary>
    public Func<T, IEnumerable<RelicModel>>? Variants { get; init; }

    private readonly T _model = ModelDb.Relic<T>();

    /// <inheritdoc />
    public override IEnumerable<RelicModel> AllVariants => Variants == null ? [_model.ToMutable()] : Variants(_model);

    /// <inheritdoc />
    public override RelicModel ModelForOption
    {
        get
        {
            var mutableModel = _model.ToMutable() as T ??
                               throw new InvalidOperationException(
                                   $"RelicModel ToMutable for {_model.GetType()} did not produce instance of {typeof(T)}");
            
            return ModelPrep?.Invoke(mutableModel) ?? mutableModel;
        }
    }
}