// Local development orchestration: an MQTT broker, the SBM API (a fake by default) and the bridge.
// Run with `aspire start` from the repository root.
//
// By default the bridge talks to a WireMock fake that serves the anonymized real SBM responses, so no account is
// needed. To use the real SBM cloud instead, set the AppHost user secrets
//   Parameters:sbm-username and Parameters:sbm-password
// (e.g. `dotnet user-secrets set Parameters:sbm-username you@example.com --project src/SbmFizikusToMqtt.AppHost`).

var builder = DistributedApplication.CreateBuilder(args);

var mqtt = builder.AddMosquitto("mqtt");

// The bridge keeps its production configuration keys; the AppHost only supplies the values.
var bridge = builder.AddProject<Projects.SbmFizikusToMqtt_Host>("bridge")
    .WithEnvironment("MqttConnector__MqttServer__Host", mqtt.Resource.Host)
    .WithEnvironment("MqttConnector__MqttServer__Port", mqtt.Resource.Port)
    .WithEnvironment("SbmConnector__PollingIntervalSeconds", "30")
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(mqtt);

var useRealSbm = !string.IsNullOrEmpty(builder.Configuration["Parameters:sbm-username"]);
if (useRealSbm)
{
    var sbmUsername = builder.AddParameter("sbm-username", secret: true);
    var sbmPassword = builder.AddParameter("sbm-password", secret: true);

    bridge
        .WithEnvironment("SbmConnector__Username", sbmUsername)
        .WithEnvironment("SbmConnector__Password", sbmPassword);
}
else
{
    var sbmMock = builder.AddContainer("sbm-mock", "wiremock/wiremock", "3.10.0")
        .WithBindMount(Path.Combine(builder.AppHostDirectory, "../../dev/wiremock/mappings"),
            "/home/wiremock/mappings", isReadOnly: true)
        .WithBindMount(Path.Combine(builder.AppHostDirectory, "../SbmFizikusToMqtt.Infrastructure.Tests/TestData/sbm"),
            "/home/wiremock/__files", isReadOnly: true)
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithHttpHealthCheck("/__admin/health");

    bridge
        .WithEnvironment("SbmConnector__BaseUrl", sbmMock.GetEndpoint("http"))
        .WithEnvironment("SbmConnector__Username", "dev@example.com")
        .WithEnvironment("SbmConnector__Password", "dev")
        .WaitFor(sbmMock);
}

builder.Build().Run();