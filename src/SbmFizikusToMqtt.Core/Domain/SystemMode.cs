namespace SbmFizikusToMqtt.Core.Domain;

/// <summary>
///     Operation mode of the building's central system. By specification it is always heating or cooling;
///     <see cref="Unknown" /> means SBM reported an unrecognized mode, not that the system is off.
/// </summary>
public enum SystemMode
{
    Heating,
    Cooling,
    Unknown
}