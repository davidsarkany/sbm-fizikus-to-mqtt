using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Response;

internal sealed record SbmApartmentInfoResponse
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("forwardWaterTemperature1DegC")]
    public required double ForwardWaterTemperature1DegC { get; init; }

    [JsonPropertyName("returningWaterTemperature1DegC")]
    public required double ReturningWaterTemperature1DegC { get; init; }

    [JsonPropertyName("flowRateLiterPerHour1")]
    public required double FlowRateLiterPerHour1 { get; init; }

    [JsonPropertyName("forwardWaterTemperature2DegC")]
    public required double ForwardWaterTemperature2DegC { get; init; }

    [JsonPropertyName("returningWaterTemperature2DegC")]
    public required double ReturningWaterTemperature2DegC { get; init; }

    [JsonPropertyName("flowRateLiterPerHour2")]
    public required double FlowRateLiterPerHour2 { get; init; }

    [JsonPropertyName("heatingHeatQuantity1KWh")]
    public required double HeatingHeatQuantity1KWh { get; init; }

    [JsonPropertyName("coolingHeatQuantity1KWh")]
    public required double CoolingHeatQuantity1KWh { get; init; }

    [JsonPropertyName("heatingCoolingVolumeMeter1M3")]
    public required double HeatingCoolingVolumeMeter1M3 { get; init; }

    [JsonPropertyName("heatingHeatQuantity2KWh")]
    public required double HeatingHeatQuantity2KWh { get; init; }

    [JsonPropertyName("coolingHeatQuantity2KWh")]
    public required double CoolingHeatQuantity2KWh { get; init; }

    [JsonPropertyName("heatingCoolingVolumeMeter2M3")]
    public required double HeatingCoolingVolumeMeter2M3 { get; init; }

    [JsonPropertyName("hotWaterHeatingQuantity1KWh")]
    public required double HotWaterHeatingQuantity1KWh { get; init; }

    [JsonPropertyName("hotWaterVolumeMeter1M3")]
    public required double HotWaterVolumeMeter1M3 { get; init; }

    [JsonPropertyName("hotWaterHeatingQuantity2KWh")]
    public required double HotWaterHeatingQuantity2KWh { get; init; }

    [JsonPropertyName("hotWaterVolumeMeter2M3")]
    public required double HotWaterVolumeMeter2M3 { get; init; }

    [JsonPropertyName("coldWaterVolumeMeter1M3")]
    public required double ColdWaterVolumeMeter1M3 { get; init; }

    [JsonPropertyName("coldWaterVolumeMeter2M3")]
    public required double ColdWaterVolumeMeter2M3 { get; init; }

    [JsonPropertyName("lastMeterSynchronisation")]
    public required string LastMeterSynchronisation { get; init; }

    [JsonPropertyName("lastStateUpdate")] public required DateTimeOffset LastStateUpdate { get; init; }

    [JsonPropertyName("fwVer")] public required string FwVer { get; init; }

    [JsonPropertyName("operationMode")] public required int OperationMode { get; init; }

    [JsonPropertyName("dewPointOffinitDegC")]
    public double DewPointOffinitDegC { get; init; }

    [JsonPropertyName("dehumidificationEquipment")]
    public required int DehumidificationEquipment { get; init; }

    [JsonPropertyName("thermostats")] public required IEnumerable<Thermostat> Thermostats { get; init; }

    [JsonPropertyName("communicationActiveRelayModule")]
    public required bool CommunicationActiveRelayModule { get; init; }

    [JsonPropertyName("communicationActiveThermostats")]
    public required bool CommunicationActiveThermostats { get; init; }

    internal sealed record Thermostat
    {
        [JsonPropertyName("ID")] public required int Id { get; init; }

        [JsonPropertyName("thermostatNo")] public required int ThermostatNo { get; init; }

        [JsonPropertyName("name")] public required string? Name { get; init; }

        [JsonPropertyName("configUpdatedByWebapp")]
        public required bool ConfigUpdatedByWebapp { get; init; }

        [JsonPropertyName("temperatureSetpointDegC")]
        public required double TemperatureSetpointDegC { get; init; }

        [JsonPropertyName("condensationRiskLevel")]
        public required double CondensationRiskLevel { get; init; }

        [JsonPropertyName("active")] public required bool Active { get; init; }

        [JsonPropertyName("measuredTempDegC")] public required double MeasuredTempDegC { get; init; }

        [JsonPropertyName("measuredHumPerc")] public required double MeasuredHumPerc { get; init; }

        [JsonPropertyName("dewPointDegC")] public required double DewPointDegC { get; init; }

        [JsonPropertyName("lastStateUpdate")] public required DateTimeOffset LastStateUpdate { get; init; }
    }
}