# Changelog

All notable changes to SBM Fizikus to MQTT will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.5]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/USER/sbm-fizikus-to-mqtt/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/USER/sbm-fizikus-to-mqtt/releases/tag/v1.0.0
