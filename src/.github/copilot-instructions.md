# GitHub Copilot Instructions for SBM Fizikus to MQTT

## Project overview

A .NET 10 / C# bridge between the SBM Fizikus heating cloud and an MQTT broker, with Home Assistant device-based discovery. Solution: `src/SbmFizikusToMqtt.slnx`.

## Architecture (ports and adapters)

| Project | Contains | May depend on |
|---|---|---|
| `SbmFizikusToMqtt.Core` | Domain, ports (`ISbmGateway`, `IMqttGateway`), `BridgeProcessor`, payload mapping, discovery | BCL and `Microsoft.Extensions.Logging.Abstractions` only |
| `SbmFizikusToMqtt.Infrastructure` | `SbmClient`/`SbmSession` (SBM API) and `MqttGateway` (MQTTnet) | Core |
| `SbmFizikusToMqtt.Host` | Configuration binding and validation, health checks, `BridgeHostedService` | Core, Infrastructure, ServiceDefaults |
| `SbmFizikusToMqtt.ServiceDefaults` | Aspire service defaults: OpenTelemetry (exported only with `OTEL_EXPORTER_OTLP_ENDPOINT`), health checks, service discovery. No default HTTP resilience handler, because the SBM client registers its own. | – |
| `SbmFizikusToMqtt.AppHost` | Aspire AppHost for local development: Mosquitto, the SBM fake (or the real SBM via user secrets) and the bridge. Run with `aspire run` or `aspire start`, never `dotnet run`. | Host |

- All bridge behaviour belongs in Core and must stay free of I/O, timers and hosting. `ArchitectureTests` enforces the dependency rules.
- `BridgeProcessor` handles one `BridgeCommand` at a time from `BridgeCommandQueue`. Do not call it concurrently.
- The configuration keys (`SbmConnector__*`, `MqttConnector__MqttServer__*`, `PublisherConfiguration__*`) are a public contract; do not rename them.
- Home Assistant `unique_id`s are a public contract; changing them orphans users' entities.

## Code conventions

- Nullable reference types, implicit usings, `TreatWarningsAsErrors`, central package versions in `src/Directory.Packages.props`.
- Models and options: `public sealed record` with `required`/`init` properties (Host settings bound from configuration: `{ get; set; }`, see AGENTS.md). Native AOT: no runtime reflection; trim and AOT warnings fail the build. Services: `internal sealed class` with primary-constructor DI, unless the host must construct them.
- Every async method takes and forwards a `CancellationToken`.
- JSON: `System.Text.Json` with source-generated contexts and `[JsonPropertyName]`. Never Newtonsoft.
- SBM failures throw `SbmException`, `SbmInvalidResponseException` (wrap `JsonException`) or `SbmApiException`.
- Use invariant culture for formatting and parsing.
- Files are UTF-8 with BOM, LF line endings, no final newline (see `src/.editorconfig`).

## Tests

- xUnit v3. Name tests `MethodName_Scenario_ExpectedResult` and mark `// Arrange`, `// Act`, `// Assert`.
- Prefer hand-written fakes at the ports over mocks. Use `FakeTimeProvider` instead of delays.
- Wire formats (MQTT payloads, discovery configs, SBM requests) are pinned with `Snapshot.Match` golden files (`*.verified.txt`). Review every diff; accept with `SNAPSHOT_ACCEPT=1`.
- SBM responses come from the anonymized real fixtures in `src/SbmFizikusToMqtt.Infrastructure.Tests/TestData/sbm`. Never commit real tokens or personal data.
- The MQTT adapter is tested against an in-process MQTTnet broker. `IntegrationTests` run the whole host against Mosquitto (Testcontainers, needs Docker) and a WireMock SBM fake.
- CI enforces line coverage: Core ≥ 90 %, Infrastructure ≥ 80 % (`dotnet test src /p:CollectCoverage=true`).

## Commands

```powershell
dotnet build src\SbmFizikusToMqtt.slnx
dotnet test src\SbmFizikusToMqtt.slnx
dotnet test src\SbmFizikusToMqtt.slnx --filter "Category!=Integration"
dotnet format src\SbmFizikusToMqtt.slnx --verify-no-changes
```
