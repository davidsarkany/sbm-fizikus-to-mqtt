# SBM Fizikus to MQTT

A .NET 10 bridge between the SBM Fizikus (SBM-Verting) heating and cooling cloud and an MQTT broker, with Home Assistant auto-discovery.

## Overview

The bridge logs in to the SBM Fizikus cloud, polls the apartment and its thermostats, and publishes their state to MQTT. It accepts target temperature commands over MQTT and forwards them to SBM. Home Assistant discovers every thermostat as a device with a climate entity and optional sensors.

## Features

- **Thermostat control**: Home Assistant climate entities for every thermostat. The target temperature can be set in 0.5 °C steps between 5 and 35 °C.
- **Sensors**: temperature, target temperature, humidity and operating state per thermostat. Central system mode, outdoor temperature and outdoor humidity for the building. All of them are opt-in.
- **Retained state**: Home Assistant and other subscribers see the current values immediately, without waiting for the next poll.
- **Accurate availability**: entities are unavailable when the bridge is down *or* SBM reports the apartment as disconnected.
- **Self-healing**: lost MQTT connections are re-established automatically, and transient SBM API errors are retried.
- **Health checks**: `/health/live` and `/health/ready` endpoints, plus a Docker `HEALTHCHECK`.
- **Small footprint**: compiled with Native AOT. The image has no .NET runtime: it is about 48 MB (a JIT build: about 180 MB), and the bridge uses about 20 MB of memory.

## Home Assistant integration

The bridge uses MQTT **device-based discovery**, so it needs a Home Assistant version that supports it. Each thermostat becomes a device named after its room, linked to an *SBM Fizikus* device for the building.

| Entity | Device | Enabled by | Default |
|---|---|---|---|
| Climate (current and target temperature, humidity, heating/cooling, idle) | thermostat | `ClimateDiscoveryEnabled` | on |
| Temperature | thermostat | `ThermostatTemperatureDiscoveryEnabled` | off |
| Target temperature | thermostat | `ThermostatTargetTemperatureDiscoveryEnabled` | off |
| Humidity | thermostat | `ThermostatHumidityDiscoveryEnabled` | off |
| System mode (diagnostic) | thermostat | `ThermostatSystemModeDiscoveryEnabled` | off |
| System mode (diagnostic) | SBM Fizikus | `ApartmentSystemModeDiscoveryEnabled` | off |
| Outdoor temperature | SBM Fizikus | `ApartmentOutdoorTemperatureDiscoveryEnabled` | off |
| Outdoor humidity | SBM Fizikus | `ApartmentOutdoorHumidityDiscoveryEnabled` | off |

The HVAC mode (heat or cool) is read-only, because the building's central system decides it.

Turning an entity off removes it from Home Assistant on the next start.

## Installation

### Docker (recommended)

**Prerequisites:** Docker with Docker Compose, an MQTT broker (e.g. Mosquitto) and an SBM Fizikus account.

```yaml
services:
  sbm-integration:
    image: ghcr.io/davidsarkany/sbm-fizikus-to-mqtt:3
    container_name: sbm-integration
    restart: unless-stopped
    environment:
      - TZ=Europe/Budapest
      # SBM Fizikus account
      - SbmConnector__Username=your_sbm_username
      - SbmConnector__Password=your_sbm_password
      # MQTT broker
      - MqttConnector__MqttServer__Host=mqtt.example.com
      - MqttConnector__MqttServer__Username=mqtt_user
      - MqttConnector__MqttServer__Password=mqtt_password
      # Optional sensors
      - PublisherConfiguration__ThermostatTemperatureDiscoveryEnabled=true
      - PublisherConfiguration__ApartmentOutdoorTemperatureDiscoveryEnabled=true
```

Start it with `docker compose up -d`.

### Configuration

All settings are environment variables. The names are unchanged from v2, so existing compose files keep working.

| Environment variable | Description | Default | Required |
|---|---|---|---|
| `SbmConnector__Username` | SBM Fizikus username or e-mail | – | ✔ |
| `SbmConnector__Password` | SBM Fizikus password (sent only as a SHA-256 hash) | – | ✔ |
| `SbmConnector__PollingIntervalSeconds` | Seconds between SBM polls | `120` | |
| `SbmConnector__OutdoorWeatherEnabled` | Fetch outdoor temperature and humidity (implied by the outdoor sensors) | `false` | |
| `SbmConnector__BaseUrl` | SBM API address | SBM cloud | |
| `MqttConnector__MqttServer__Host` | MQTT broker host name | – | ✔ |
| `MqttConnector__MqttServer__Port` | MQTT broker port | `1883` | |
| `MqttConnector__MqttServer__Username` | MQTT user name (anonymous when empty) | – | |
| `MqttConnector__MqttServer__Password` | MQTT password | – | |
| `MqttConnector__MqttServer__ClientId` | MQTT client id | `sbm-fizikus-mqtt` | |
| `PublisherConfiguration__SbmTopic` | Base topic of the bridge | `sbm_fizikus` | |
| `PublisherConfiguration__HomeAssistantTopic` | Home Assistant discovery prefix | `homeassistant` | |
| `PublisherConfiguration__*DiscoveryEnabled` | Entity switches, see [Home Assistant integration](#home-assistant-integration) | | |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry endpoint for logs, traces and metrics, see [Observability](#observability) | – (export off) | |
| `OTEL_SERVICE_NAME` | Service name in the telemetry | `sbm-fizikus-to-mqtt` | |
| `Logging__LogLevel__Default` | Minimum log level, see [Logging](#logging) | `Information` | |
| `Logging__LogLevel__{Category}` | Minimum log level of a category and its children, e.g. `Logging__LogLevel__SbmFizikusToMqtt=Debug` | `Warning` for `Microsoft`, `System` and `Polly` | |

Invalid or missing required settings stop the application at startup with exit code 1 and a log message naming the setting.

The bridge handles the first building and the first apartment of the SBM account. If the account has access to more, it logs a warning.

## Upgrading from v2

The configuration keys are unchanged, but the v2 Home Assistant discovery configs are **not migrated** and have to be removed by hand.

1. Stop the v2 container.
2. **Remove the v2 discovery configs.** v3 publishes its entities with the same `unique_id`s under new device-based topics. As long as the v2 configs are still retained on the broker, Home Assistant rejects the new entities as duplicates. Either delete the SBM Fizikus MQTT entities in Home Assistant (*Settings → Devices & services → MQTT*), or clear the v2 configs on the broker. All v2 discovery topics contain `sbm_fizikus-` (with a hyphen); v3 uses `…/device/sbm_fizikus_…` and is not affected:

   ```bash
   mosquitto_sub -h <broker> -u <user> -P <password> -v --retained-only -W 3 \
     -t 'homeassistant/+/+/config' -t 'homeassistant/+/+/+/config' \
     | grep 'sbm_fizikus-' | cut -d' ' -f1 \
     | xargs -I{} mosquitto_pub -h <broker> -u <user> -P <password> -r -n -t {}
   ```

   The entities are then recreated by v3. Their entity ids, history and settings are not guaranteed to be kept.
3. **Replace the Serilog configuration.** v3 does not use Serilog, so all `Serilog__*` variables are ignored, including the Loki sink (`Serilog__Using__*` / `Serilog__WriteTo__*`) and the log levels (`Serilog__MinimumLevel__*`). Remove them. Send logs to Grafana / Loki with OpenTelemetry instead (see [Observability](#observability)), and set log levels with `Logging__LogLevel__*` (see [Logging](#logging)).
4. Change the image tag to `ghcr.io/davidsarkany/sbm-fizikus-to-mqtt:3` and start it.
5. The entities move under one device per thermostat. Their display names change accordingly (e.g. *living-room* for the climate entity, *living-room Temperature* for the sensor).

To stay on v2, pin the image tag `:2`.

## MQTT interface

`{SbmTopic}` defaults to `sbm_fizikus`, and `{HomeAssistantTopic}` defaults to `homeassistant`.

| Topic | Direction | Retained | Content |
|---|---|---|---|
| `{SbmTopic}/bridge/state` | publish | yes | `{"state": "online"}` while the bridge runs, `{"state": "offline"}` after it stops (also the last will) |
| `{SbmTopic}/apartment_info` | publish | yes | `system_mode`, `last_update`, `connected`, optional `outdoor_temperature` and `outdoor_humidity` |
| `{SbmTopic}/devices/{id}` | publish | yes | `id`, `name`, `temperature`, `humidity`, `target_temperature`, `system_mode` (`heating`, `cooling`, `unknown` or `idle`), `last_update` |
| `{HomeAssistantTopic}/device/{SbmTopic}_thermostat_{id}/config` | publish | yes | Discovery config of a thermostat |
| `{HomeAssistantTopic}/device/{SbmTopic}_apartment/config` | publish | yes | Discovery config of the building sensors |
| `{SbmTopic}/devices/{id}/set` | subscribe | | `{"value": 21.5}` or `{"id": 1763, "value": 21.5}` |
| `{HomeAssistantTopic}/status` | subscribe | | `online` makes the bridge republish its state and discovery |

Example state:

```json
{"system_mode":"heating","last_update":"2026-10-10T12:45:11.578277+00:00","connected":true,"outdoor_temperature":19.9,"outdoor_humidity":49.9}
```

In discovery topics, characters of `{SbmTopic}` other than letters, digits, `_` and `-` are replaced with `_`. All timestamps are UTC. The bridge rejects a temperature command, and logs a warning, when any of these holds:

- the id in the payload differs from the id in the topic;
- the thermostat is unknown;
- the value is outside 5–35 °C or not a multiple of 0.5;
- the command is retained. A retained command would be replayed on every reconnect and override later changes; clear it by publishing an empty retained message to the topic.

When SBM stops reporting a thermostat, the bridge clears its retained discovery config and state, so Home Assistant removes the device. An empty thermostat list from SBM is treated as a glitch and removes nothing.

## Health checks

The container listens on port 8080:

- `GET /health/live`: the process is running.
- `GET /health/ready`: the MQTT broker is connected and SBM was polled successfully within the last three polling intervals.

The Docker image's `HEALTHCHECK` uses `/health/ready`.

## Observability

Logs, traces and metrics are exported with OpenTelemetry (OTLP) when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Without it, the bridge only writes logs to the console.

### Logging

The bridge writes one line per log entry to the console (`docker logs`), followed by the exception if there is one:

```text
[06:18:55 INF] Connected to MQTT broker and subscribed to sbm_fizikus/devices/+/set and homeassistant/status <s:SbmFizikusToMqtt.Infrastructure.Mqtt.MqttGateway>
```

The levels are `TRC`, `DBG`, `INF`, `WRN`, `ERR` and `CRT`. The time is the container's local time, set with `TZ` (e.g. `TZ=Europe/Budapest`); without it, it is UTC.

Every successful refresh (poll, reconnect, Home Assistant restart or temperature command) logs a summary:

```text
[14:03:27 INF] Refreshed state (poll): 5 thermostats, 2 calling, heating, SBM connected, data age 42 s <s:SbmFizikusToMqtt.Core.Bridge.BridgeProcessor>
```

The values are also exported as attributes: `Trigger`, `ThermostatCount`, `ActiveCount`, `SystemMode`, `SbmConnection` and `DataAgeSeconds`. Timings are in the traces (e.g. the `bridge poll` span), not in the log. `DataAgeSeconds` is the age of SBM's `lastStateUpdate`. If it keeps growing, SBM answers with old data (e.g. the building's relay module is offline), so it is a useful alert condition, e.g. in Loki: `{service_name="sbm-fizikus-to-mqtt"} | DataAgeSeconds > 900`.

Log levels use the standard .NET logging configuration and apply to both the console and the OpenTelemetry export:

```yaml
    environment:
      - Logging__LogLevel__Default=Information          # everything
      - Logging__LogLevel__SbmFizikusToMqtt=Debug       # the bridge's own categories
      - Logging__LogLevel__Polly=Information            # SBM retries (default: Warning)
```

With OpenTelemetry export on, every log entry is also sent as an OTLP log record, with the message template's values as attributes.

- **Traces:** each handled event is one trace, e.g. `bridge poll` with its `SBM get_apartment_data` call and HTTP request, or `bridge set_temperature`.
- **Service name:** the default is `sbm-fizikus-to-mqtt`, together with the application version. `OTEL_SERVICE_NAME` or `OTEL_RESOURCE_ATTRIBUTES` override it.

### Grafana Cloud through Grafana Alloy (recommended)

Grafana recommends sending through a local Grafana Alloy rather than directly from the application. Alloy buffers and retries when Grafana Cloud is unreachable, and the Grafana Cloud token stays in Alloy instead of the bridge container.

Bridge container:

```yaml
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://host.docker.internal:4317   # or http://alloy:4317 in the same compose network
      - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
    extra_hosts:
      - "host.docker.internal:host-gateway"   # Linux: lets the container reach Alloy on the host
```

Alloy (`config.alloy`): receive OTLP and forward it to Grafana Cloud. The endpoint, instance ID and token come from your stack's **OpenTelemetry** tile. If Alloy already exports to Grafana Cloud, connect the receiver to the existing exporter instead.

```alloy
otelcol.receiver.otlp "default" {
  grpc { endpoint = "0.0.0.0:4317" }
  http { endpoint = "0.0.0.0:4318" }
  output {
    logs    = [otelcol.processor.batch.default.input]
    metrics = [otelcol.processor.batch.default.input]
    traces  = [otelcol.processor.batch.default.input]
  }
}

otelcol.processor.batch "default" {
  output {
    logs    = [otelcol.exporter.otlphttp.grafana_cloud.input]
    metrics = [otelcol.exporter.otlphttp.grafana_cloud.input]
    traces  = [otelcol.exporter.otlphttp.grafana_cloud.input]
  }
}

otelcol.auth.basic "grafana_cloud" {
  username = sys.env("GRAFANA_CLOUD_INSTANCE_ID")
  password = sys.env("GRAFANA_CLOUD_API_TOKEN")
}

otelcol.exporter.otlphttp "grafana_cloud" {
  client {
    endpoint = "https://otlp-gateway-<REGION>.grafana.net/otlp"
    auth     = otelcol.auth.basic.grafana_cloud.handler
  }
}
```

The OTLP receiver listens on all interfaces (`0.0.0.0`). If the host is reachable from other networks, restrict access with a firewall.

### Grafana Cloud directly

Without Alloy, use the values from the OpenTelemetry tile:

```yaml
      - OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp-gateway-<REGION>.grafana.net/otlp
      - OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
      - OTEL_EXPORTER_OTLP_HEADERS=Authorization=Basic%20<base64 of instanceID:token>
```

## Resilience

- **MQTT:** the bridge reconnects with exponential backoff (1 s up to 60 s). After every reconnect it republishes its state and discovery, because a broker without persistence loses retained messages.
- **SBM:** transient errors are retried. A rejected session triggers one fresh login. A failed poll is logged and retried on the next one.

## Development

Requirements: .NET 10 SDK. The integration tests also need Docker.

The bridge is published with **Native AOT**. `dotnet build`, `dotnet run`, the tests and Aspire run it as a normal (JIT) .NET application, so no extra tooling is needed for day-to-day work. The Native AOT executable is built by the Dockerfile in the `sdk:10.0-alpine-aot` image, which contains the native toolchain. A local `dotnet publish src\SbmFizikusToMqtt.Host -r win-x64` also works, but needs the Visual Studio *Desktop development with C++* workload.

Native AOT allows no runtime reflection or code generation. Every build runs the trim and AOT analyzers, and with `TreatWarningsAsErrors` a warning fails the build (in the Docker publish too). What this means in practice:

- JSON uses source-generated `JsonSerializerContext`s only.
- Settings are bound by the configuration binding source generator, so the settings classes need **setters** (`{ get; set; }`). The generator silently skips `init` accessors.
- Settings are validated by `[OptionsValidator]` source-generated validators (`BridgeSettingsValidators.cs`), not by `ValidateDataAnnotations()`.
- A new dependency must be trim and AOT compatible. Check with `docker build .`, because some warnings appear only at publish.

```powershell
dotnet build src\SbmFizikusToMqtt.slnx
dotnet test src\SbmFizikusToMqtt.slnx                              # all tests (integration tests need Docker)
dotnet test src\SbmFizikusToMqtt.slnx --filter "Category!=Integration"
dotnet test src\SbmFizikusToMqtt.slnx /p:CollectCoverage=true      # with coverage gates
dotnet format src\SbmFizikusToMqtt.slnx --verify-no-changes
```

### Running locally with Aspire

The [Aspire](https://aspire.dev) AppHost (`src/SbmFizikusToMqtt.AppHost`) starts everything needed for development:

- a Mosquitto broker;
- a fake SBM API that serves anonymized real responses;
- the bridge, with its logs, traces and metrics in the Aspire dashboard.

It requires the Aspire CLI and Docker.

```bash
aspire run --apphost src/SbmFizikusToMqtt.AppHost      # interactive, opens the dashboard
aspire start --apphost src/SbmFizikusToMqtt.AppHost    # in the background
```

To develop against the real SBM cloud instead of the fake, set the AppHost user secrets:

```bash
dotnet user-secrets set Parameters:sbm-username you@example.com --project src/SbmFizikusToMqtt.AppHost
dotnet user-secrets set Parameters:sbm-password your_password --project src/SbmFizikusToMqtt.AppHost
```

The AppHost is for local development only; the Docker image does not include it. The bridge exports OpenTelemetry only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, as Aspire does.

### Running the Docker image locally without an SBM account

`docker-compose.dev.yml` starts Mosquitto, a fake SBM API that serves anonymized real responses, and the bridge built from source:

```bash
docker compose -f docker-compose.dev.yml up --build
docker compose -f docker-compose.dev.yml exec mosquitto mosquitto_sub -v -t '#'
curl http://localhost:8081/health/ready
```

### Project structure

```
src/
├── SbmFizikusToMqtt.Core/                  # Bridge logic, domain, payloads and discovery (no I/O)
├── SbmFizikusToMqtt.Infrastructure/        # SBM API and MQTT adapters
├── SbmFizikusToMqtt.Host/                  # Configuration, health checks, hosted bridge loop
├── SbmFizikusToMqtt.ServiceDefaults/       # OpenTelemetry, default health checks, service discovery
├── SbmFizikusToMqtt.AppHost/               # Aspire local orchestration (development only)
├── SbmFizikusToMqtt.Core.Tests/            # Unit, snapshot and component tests
├── SbmFizikusToMqtt.Infrastructure.Tests/  # Adapter tests (fixtures, in-process MQTT broker)
├── SbmFizikusToMqtt.Host.Tests/            # Settings binding and validation, console log format
├── SbmFizikusToMqtt.IntegrationTests/      # Whole host against Mosquitto and a fake SBM API
└── TestShared/                             # Golden-file snapshot helper
```

The dependencies point inwards: Host → Infrastructure → Core. Core has no transport or hosting dependencies; an architecture test enforces this.

## Troubleshooting

- **Entities unavailable:** check `{SbmTopic}/bridge/state`, and the `connected` field of `{SbmTopic}/apartment_info`. `false` means SBM itself reports the relay module or the thermostats as unreachable.
- **No data:** check `/health/ready` and the logs. SBM API errors include the response returned by the API.
- **Entities not discovered:** make sure Home Assistant's MQTT integration uses the same discovery prefix as `PublisherConfiguration__HomeAssistantTopic`.
