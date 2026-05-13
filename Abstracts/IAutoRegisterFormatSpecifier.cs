
using SmartFormat.Core.Extensions;

namespace BaseLib.Abstracts;

/// <summary>
/// A formatter for SmartFormat that will automatically be added to the game's default
/// formatter. The formatter class should have a parameterless constructor.
/// It will be added to Smart.Default after all mods are initialized.
/// </summary>
public interface IAutoRegisterFormatSpecifier : IFormatter;