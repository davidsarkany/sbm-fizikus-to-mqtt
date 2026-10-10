# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project overview

.NET 10 / C# bridge between the SBM Fizikus heating cloud and an MQTT broker. It publishes retained thermostat and apartment state, accepts temperature commands over MQTT and publishes Home Assistant device-based discovery. Solution file: `src/SbmFizikusToMqtt.slnx`. Requirements baseline: `docs/requirements-specification.md`.

## Solution layout (`src/`)

| Project | Purpose | Depends on |
|---|---|---|
| `SbmFizikusToMqtt.Core` | Domain, ports (`ISbmGateway`, `IMqttGateway`), `BridgeProcessor`, `BridgeCommandQueue`, state payloads and discovery. No I/O, timers or hosting. | BCL, `Logging.Abstractions` |
| `SbmFizikusToMqtt.Infrastructure` | Adapters: `Sbm/` (`SbmClient`, `SbmSession`, DTOs, JSON converters) and `Mqtt/` (`MqttGateway`) | Core |
| `SbmFizikusToMqtt.Host` | Composition root, published with Native AOT: settings binding and validation (`Configuration/`), console logging (`Logging/`), `BridgeHostedService`, health checks (`Health/`) | Core, Infrastructure, ServiceDefaults |
| `SbmFizikusToMqtt.ServiceDefaults` | Aspire service defaults: OpenTelemetry (exported only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set), default health checks, service discovery. Intentionally no default HTTP resilience handler, because the SBM client has its own. | |
| `SbmFizikusToMqtt.AppHost` | Aspire AppHost for local development: Mosquitto (CommunityToolkit), the WireMock SBM fake (or the real SBM via user secrets `Parameters:sbm-username`/`sbm-password`) and the bridge | Host |
| `SbmFizikusToMqtt.Core.Tests` | Unit, snapshot and component tests with hand-written fakes (`TestSupport/`) | |
| `SbmFizikusToMqtt.Infrastructure.Tests` | Adapter tests: SBM fixtures (`TestData/sbm`), in-process MQTT broker | |
| `SbmFizikusToMqtt.Host.Tests` | Host unit tests without Docker: settings binding and validation, console log format | |
| `SbmFizikusToMqtt.IntegrationTests` | Whole host against Mosquitto (Testcontainers) and a WireMock SBM fake | |
| `TestShared/` | `Snapshot` golden-file helper, linked into every test project | |

Dependencies point inwards, and `ArchitectureTests` keeps transport and hosting libraries out of Core.

## Commands

Run from the repository root (PowerShell; chain with `;`, not `&&`):

```powershell
dotnet build src\SbmFizikusToMqtt.slnx
dotnet test src\SbmFizikusToMqtt.slnx                                    # integration tests need Docker
dotnet test src\SbmFizikusToMqtt.slnx --filter "Category!=Integration"
dotnet test src\SbmFizikusToMqtt.slnx /p:CollectCoverage=true            # CI coverage gates
dotnet format src\SbmFizikusToMqtt.slnx --verify-no-changes              # CI check; drop the flag to apply fixes
aspire run --apphost src/SbmFizikusToMqtt.AppHost                         # local dev with the Aspire dashboard
docker compose -f docker-compose.dev.yml up --build                      # local E2E without an SBM account
```

## Contracts that must not change by accident

- Configuration keys: `SbmConnector__*`, `MqttConnector__MqttServer__*`, `PublisherConfiguration__*`.
- Home Assistant `unique_id`s (`sbm_fizikus-{id}_climate` etc.); changing them orphans users' entities.
- MQTT topics and payloads, pinned by `*.verified.txt` snapshots. Review every snapshot diff.
- SBM API request bodies, pinned by `SbmClientTests`.

## Code conventions

- Nullable reference types, implicit usings and `TreatWarningsAsErrors` are on. Package versions are central in `src/Directory.Packages.props`.
- Models, DTOs and options: `public sealed record` with `required` + `init` properties. Exception: Host settings bound from configuration (`Configuration/BridgeSettings.cs`) use `{ get; set; }` and `[Required]`, because the configuration binding source generator silently skips `init` accessors.
- Native AOT: the Host is published with `PublishAot`, and the libraries it references (Core, Infrastructure, ServiceDefaults) are `IsAotCompatible`. No runtime reflection or code generation: source-generated JSON contexts, configuration binding and `[OptionsValidator]` validators; no `ValidateDataAnnotations()`, no `Serilog.Settings.Configuration`-style reflection-based libraries. Trim and AOT warnings fail the build; some appear only at publish, so check new dependencies with `docker build .`.
- Services: `internal sealed class` with primary-constructor DI. Make them public only when the host must construct them (e.g. `BridgeProcessor`).
- `BridgeProcessor` is single-threaded by design: feed it only through `BridgeCommandQueue`.
- Every async method takes and forwards a `CancellationToken`.
- JSON: `System.Text.Json` with source-generated contexts and `[JsonPropertyName]`; never Newtonsoft.
- SBM failures: `SbmException`, `SbmInvalidResponseException` (wrap `JsonException`), `SbmApiException` (HTTP status).
- Use invariant culture for formatting and parsing.
- Files: UTF-8 with BOM, LF line endings, no final newline (`src/.editorconfig`).
- Extension classes are named `{TypeExtended}Extensions` in `{TypeExtended}Extensions.cs`.

## Testing

- xUnit v3. Name tests `MethodName_Scenario_ExpectedResult` and mark `// Arrange`, `// Act`, `// Assert`.
- Prefer hand-written fakes at the ports over mocks, and `FakeTimeProvider` over delays. In integration tests, use `Eventually` instead of fixed sleeps.
- Golden files: `Snapshot.Match(text)` compares with `{Class}.{Method}.verified.txt`. Accept intended changes with `SNAPSHOT_ACCEPT=1`, after reviewing the diff.
- SBM fixtures are anonymized real responses. Never commit real tokens, credentials or personal data.
- Coverage gates (line): Core ≥ 90 %, Infrastructure ≥ 80 %.
- Cover positive and negative cases; add a regression test for every bug fix.

## Workflow

- Branch from `main`: `feature/…`, `fix/…`, `chore/…`. Keep commits small and focused (conventional commits, e.g. `fix: …`, `chore(deps): …`).
- Before finishing: build, run all tests and run the `dotnet format` check.
- Update `CHANGELOG.md` and `README.md` when behaviour or configuration changes.
- Never commit credentials; SBM and MQTT settings come from environment variables (see `README.md`).

See also `CONTRIBUTING.md`.

## Aspire

- Aspire skills (`aspire`, `aspireify`, `aspire-orchestration`, `aspire-monitoring`, …) live in `.agents/skills`. Follow them for anything that touches the AppHost. They are generated vendor content and are not committed (`.agents/` is git-ignored). Generate or refresh them, e.g. after an Aspire upgrade, with:

  ```powershell
  $env:ASPIRE_CLI_TELEMETRY_OPTOUT = 'true'; aspire agent init --skill-locations standard --non-interactive
  ```

  `aspire agent init` can also add an Aspire telemetry `PostToolUse` hook to the user's `~/.claude/settings.json`. Check that file afterwards and remove the hook unless the user wants it.
- The Aspire MCP server is configured in `.mcp.json` (`aspire agent mcp`) and needs the Aspire CLI installed.
- Start the AppHost with `aspire start` or `aspire run`, never `dotnet run`. Use `aspire wait <resource>`, not HTTP polling. Do not `dotnet build` while the AppHost is running; stop it first or use resource commands.
- Pass `--suppress-agent-init` to `aspire init` so it does not reinstall skills or add the Aspire telemetry hook to the user's Claude Code settings.
- The AppHost passes the bridge's production configuration keys through environment variables. Keep the Host free of Aspire-specific configuration.
