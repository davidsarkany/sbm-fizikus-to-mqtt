using Bogus;
using SbmFizikusToMqtt.Domain;

namespace SbmFizikusToMqtt.Application.Tests.Fakers;

internal static class ApartmentFakers
{
    internal static readonly Faker<Thermostat> ThermostatFaker = new Faker<Thermostat>()
        .RuleFor(x => x.Id, f => f.Random.Int(1, 1000))
        .RuleFor(x => x.Name, f => f.Name.FirstName())
        .RuleFor(x => x.Temperature, f => f.Random.Double(15, 30))
        .RuleFor(x => x.Humidity, f => f.Random.Double(30, 70))
        .RuleFor(x => x.TargetTemperature, f => f.Random.Double(18, 25))
        .RuleFor(x => x.DewPoint, f => f.Random.Double(5, 15))
        .RuleFor(x => x.Active, f => f.Random.Bool())
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset());

    internal static readonly Faker<Apartment> ApartmentFaker = new Faker<Apartment>()
        .RuleFor(x => x.SystemMode, f => f.PickRandom("heating", "cooling", "off"))
        .RuleFor(x => x.Thermostats, f => GenerateThermostats(f.Random.Int(1, 3)))
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset())
        .RuleFor(x => x.RelayConnectionActive, f => f.Random.Bool())
        .RuleFor(x => x.ThermostatsConnectionActive, f => f.Random.Bool());

    private static Thermostat[] GenerateThermostats(int count)
    {
        return ThermostatFaker.Generate(count).ToArray();
    }
}