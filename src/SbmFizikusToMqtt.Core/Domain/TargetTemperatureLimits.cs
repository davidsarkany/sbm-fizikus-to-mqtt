namespace SbmFizikusToMqtt.Core.Domain;

/// <summary>Accepted target temperature setpoints, shared by command validation and the climate entity.</summary>
public static class TargetTemperatureLimits
{
    public const double Minimum = 5;
    public const double Maximum = 35;
    public const double Step = 0.5;

    public static bool IsValid(double temperature)
    {
        if (!double.IsFinite(temperature) || temperature < Minimum || temperature > Maximum)
            return false;

        var steps = temperature / Step;
        return Math.Abs(steps - Math.Round(steps)) < 1e-9;
    }
}