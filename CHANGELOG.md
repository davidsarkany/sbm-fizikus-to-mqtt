# Changelog

All notable changes to SBM Fizikus to MQTT will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.3] - 2026-10-30

### Changed

- Updated Aspire.AppHost.Sdk from 13.6.1 to 13.6.4, MessagePack from 2.5.307 to 3.1.12, and WireMock.Net from 2.20.0 to 2.21.0.

## [3.0.2] - 2026-10-26

### Changed

- Updated Microsoft.Extensions.Hosting.Abstractions, Microsoft.Extensions.Http, Microsoft.Extensions.Logging.Abstractions and Microsoft.AspNetCore.Mvc.Testing to 10.0.14.
- Updated Microsoft.Extensions.Http.Resilience, Microsoft.Extensions.ServiceDiscovery and Microsoft.Extensions.TimeProvider.Testing to 10.11.0.
- Updated OpenTelemetry packages to 1.20.1 and CommunityToolkit.Aspire.Hosting.Mosquitto to 13.6.1.
- Updated test dependencies: Microsoft.NET.Test.Sdk to 18.11.0, Testcontainers to 4.16.0, WireMock.Net to 2.20.0, xunit.runner.visualstudio to 4.0.1 and xunit.v3 to 4.0.2.

### Fixed

- Pinned the AppHost's transitive MessagePack dependency to 2.5.307 to address GHSA-qhrr-8q5h-9q3h and the resulting package restore failure.

## [3.0.0] - 2026-10-10

Rewrite of the bridge. The configuration keys are unchanged; the v2 Home Assistant discovery configs must be removed before upgrading (see README, "Upgrading from v2").

### Breaking changes

- Home Assistant discovery now uses device-based discovery (`{HomeAssistantTopic}/device/…/config`), with one device per thermostat and an *SBM Fizikus* device for the building. **The v2 discovery configs are not migrated: remove them before upgrading, otherwise Home Assistant rejects the new entities as duplicate `unique_id`s.** Entity ids, history and settings are not guaranteed to be kept. A Home Assistant version with device-based discovery support is required.
- Entity display names follow the device: the climate entity is named after the room, and the sensors are named *Temperature*, *Humidity*, etc.
- State topics (`bridge/state`, `apartment_info`, `devices/{id}`) are published retained.
- `bridge/state` now reports whether the bridge process is running: `online` on connect, `offline` on shutdown and as the retained last will. SBM connectivity moved to the new `connected` field of `apartment_info`, and entities are available only when both are online.
- Temperature commands are validated: the payload id must match the topic id (the id is now optional), the thermostat must exist, and the value must be between 5 and 35 °C in 0.5 °C steps. Retained commands are ignored, because they would be replayed on every reconnect.
- The image is built with Native AOT: it is based on `runtime-deps` (no .NET runtime), and its entry point is the executable `./SbmFizikusToMqtt` instead of `dotnet SbmFizikusToMqtt.dll`. The container exposes port 8080 for health checks.
- Serilog is removed. Logs go to the console and, with OpenTelemetry, to `OTEL_EXPORTER_OTLP_ENDPOINT` (e.g. a local Grafana Alloy) instead of the Serilog Loki sink. All `Serilog__*` variables are ignored: log levels are set with `Logging__LogLevel__*` instead of `Serilog__MinimumLevel__*`. The console line format is unchanged, except that the levels are `TRC`/`CRT` instead of `VRB`/`FTL` and the output has no ANSI colors.

### Added

- Native AOT compilation. Compared with a JIT build on the `aspnet` image, the Docker image is about 48 MB instead of about 180 MB, and the bridge uses about 20 MB of memory instead of about 35 MB. The build fails on any trim or AOT warning.
- `/health/live` and `/health/ready` endpoints and a Docker `HEALTHCHECK`.
- Automatic MQTT reconnection with exponential backoff (1–60 s). State and discovery are republished after every reconnect.
- Retries of transient SBM API errors, and a fresh login when SBM rejects the session.
- Entities that are switched off in the configuration are removed from Home Assistant.
- Discovery is republished when the thermostats change. A thermostat that SBM no longer reports has its retained discovery config and state cleared, so Home Assistant removes it.
- A warning when the SBM account has more than one building or apartment.
- An Information log line after every refresh: trigger, thermostat count, calling zones, system mode, SBM connection and data age.
- `docker-compose.dev.yml` for running the bridge locally against Mosquitto and a fake SBM API.
- Aspire AppHost for local development (`aspire run`). It starts Mosquitto, the SBM fake (or the real SBM cloud via user secrets) and the bridge, with logs, traces and metrics in the Aspire dashboard.
- OpenTelemetry through Aspire service defaults. Export happens only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Each handled event is one trace (`bridge poll`, `bridge set_temperature`, `bridge mqtt_connected`, `bridge homeassistant_status`). It contains the SBM calls as `SBM {operation}` spans (e.g. `SBM get_apartment_data`, with the HTTP request as their child) and a `bridge publish_discovery` span. Failed events are marked as errors; rejected commands carry a `bridge.rejected` attribute. Without `OTEL_SERVICE_NAME`, the service is reported as `sbm-fizikus-to-mqtt` with the application version.

### Changed

- New architecture: `Core` (logic), `Infrastructure` (SBM and MQTT adapters) and `Host`, replacing seven projects.
- Enabling the outdoor sensor discovery now also enables fetching outdoor weather.
- MQTT username and password are optional. The default client id is `sbm-fizikus-mqtt`.
- Polls, commands and Home Assistant events are processed one at a time, so they no longer run concurrently.
- Tests: snapshot tests of the wire formats, adapter tests with anonymized real SBM responses, and integration tests against Mosquitto. Line coverage is enforced in CI.

### Fixed

- `last_update` timestamps were shifted by the container's time zone. SBM sends them in UTC without an offset, and v2 read them as local time.
- An unexpected MQTT disconnect no longer stops the application.
- A failed startup now exits with code 1.

### Removed

- Serilog (`Serilog.AspNetCore`, `Serilog.Settings.Configuration`, `Serilog.Sinks.Console` and the Loki sink `Serilog.Sinks.Loki.YetAnother`), replaced by the built-in .NET logging and OpenTelemetry export, and the AutoBogus test dependency.

## [2.1.3] - 2026-10-09

### Fixed

- Home Assistant entities not appearing after a transient SBM API outage: if auto-discovery could not be published on startup or the Home Assistant `online` (birth) message, the polling background service now retries it on every successful poll until it has been published once. Previously discovery was never retried, so a single SBM outage during those windows left the entities missing until the container was restarted.

## [2.1.2] - 2026-08-20

### Changed

- Replaced the TickerQ job scheduler with a `PeriodicTimer`-based background service (`SbmPollingBackgroundService`), eliminating constant idle CPU usage from scheduler polling
- `SbmConnector:PollingCronExpression` configuration replaced by `SbmConnector:PollingIntervalSeconds` (seconds between SBM polls)
- MQTT connection wait loops now use `PeriodicTimer` instead of `Task.Delay` polling
- MQTT connection waiting centralized in `MqttConnectionService` (`IMqttConnection.WaitUntilConnectedAsync`); background services no longer poll `IsConnected`

### Removed

- TickerQ dependency from the application, web, and test projects

## [2.1.1] - 2026-08-18

### Changed

- GitHub Actions dependency upgrades: actions/checkout v6 → v7, actions/setup-dotnet v5 → v6, docker/setup-buildx-action 4.0.0 → 4.2.0, docker/login-action 4.0.0 → 4.6.0, docker/metadata-action 6.0.0 → 6.2.0, docker/build-push-action 7.0.0 → 7.3.0, aquasecurity/trivy-action 0.35.0 → 0.36.0, sigstore/cosign-installer 4.1.1 → 4.1.2
- Test project dependency upgrades: Microsoft.NET.Test.Sdk 18.8.1 → 18.9.0, xunit.runner.visualstudio 3.1.5 → 4.0.0

## [2.1.0] - 2026-08-13

### Added

- Dependabot configuration for weekly NuGet and GitHub Actions dependency updates
- SBM API client requests now use a 30-second HTTP timeout
- SBM API errors now include the response body returned by the API in the logged exception

### Changed

- Access tokens now refresh when fewer than 30 seconds of validity remain, avoiding failed requests with near-expired tokens
- MQTTnet dependency narrowed from `MQTTnet.AspNetCore` to `MQTTnet` with explicit `Microsoft.Extensions.*` package references
- Bridge online/offline state payloads centralized in a `BridgeStatePayloads` constant class

### Fixed

- Trivy vulnerability scan now skipped on pull requests, where no image is pushed
- Removed stale root-level `workflows/` directory — CI only reads `.github/workflows/`
- Removed duplicate project COPY from the Dockerfile
- Removed unnecessary `dotnet tool restore` step from the format workflow

## [2.0.1] - 2026-08-11

### Changed

- Dependency upgrades: Microsoft.Extensions.* 10.0.10 → 10.0.11

## [2.0.0] - 2026-07-22

### Changed

- Application now immediately shuts down on unexpected MQTT disconnection, relying on the container restart policy (`restart: unless-stopped`) to perform a clean restart
- Removed exponential backoff reconnection logic in favor of the simpler restart approach — this guarantees Home Assistant auto-discovery messages and MQTT subscriptions are fully re-established after broker or network interruptions
- Removed `MqttConnector:MqttReconnect` configuration section (`MaxReconnectAttempts`, `InitialDelaySeconds`, `MaxDelaySeconds` are no longer used)

### Fixed

- Home Assistant losing discovery messages after MQTT broker restarts — previously, the service would reconnect but never re-publish discovery messages or re-subscribe to topics

## [1.1.0] - 2026-07-16

### Added

- Outdoor temperature and humidity support: `OutdoorWeatherEnabled` config flag (default `false`) enables fetching outdoor weather from the SBM building access rights API
- Outdoor data published to MQTT `apartment_info` topic as `outdoor_temperature` and `outdoor_humidity` (omitted when `null`)
- Home Assistant auto-discovery sensors for outdoor temperature and humidity (opt-in via `ApartmentOutdoorTemperatureDiscoveryEnabled` / `ApartmentOutdoorHumidityDiscoveryEnabled`)

## [1.0.5] - 2026-07-16

### Changed

- Dependency upgrades: Microsoft.Extensions.* 10.0.9 → 10.0.10, Microsoft.NET.Test.Sdk 18.7.0 → 18.8.1

## [1.0.4] - 2026-07-04

### Changed

- MQTTnet dependency upgrades (MQTTnet 5.2.0.1603, MQTTnet.AspNetCore 5.2.0.1603)

## [1.0.3] - 2026-06-27

### Changed

- Dependency upgrades across all project packages

## [1.0.2] - 2026-06-27

### Fixed

- Target temperature can now be set with 0.5°C precision (previously only whole-degree steps were accepted)

## [1.0.1] - 2026-05-24

### Changed

- Dependency upgrades across all project packages
- Test project dependency upgrades

### Fixed

- Corrected incorrect data in README documentation

## [1.0.0] - 2026-02-18

### Added

- Initial release of SBM Fizikus to MQTT
- MQTT publishing of thermostat and apartment data from SBM Fizikus heating systems
- MQTT command listener for thermostat temperature control
- Home Assistant auto-discovery integration (climate/HVAC entities, temperature, humidity, operating state)
- Docker and Docker Compose deployment support
- .NET 10 cross-platform application

[3.0.3]: https://github.com/davidsarkany/sbm-fizikus-to-mqtt/compare/v3.0.2...v3.0.3
[3.0.2]: https://github.com/davidsarkany/sbm-fizikus-to-mqtt/compare/v3.0.0...v3.0.2
[2.1.0]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v2.0.1...v2.1.0
[2.0.1]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.5...v1.1.0
[1.0.5]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/USER/sbm-fizikus-to-mqtt/releases/tag/v1.0.0