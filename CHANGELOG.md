# Changelog

All notable changes to SBM Fizikus to MQTT will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
