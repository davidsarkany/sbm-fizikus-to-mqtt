# Changelog

All notable changes to SBM Fizikus to MQTT will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[2.0.1]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.5...v1.1.0
[1.0.5]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/USER/sbm-fizikus-to-mqtt/releases/tag/v1.0.0
