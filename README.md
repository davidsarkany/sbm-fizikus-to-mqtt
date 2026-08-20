# SBM Fizikus to MQTT

A .NET 10 application that bridges SBM-Verting heating systems with MQTT, enabling integration with home automation systems like Home Assistant.

## Overview

This project connects to an SBM Fizikus heating system cloud application, retrieves thermostat and apartment data, and publishes it to an MQTT broker. It supports thermostat temperature control via MQTT commands and includes Home Assistant auto-discovery for seamless integration.
## Features

- **MQTT Publishing**: Publishes data to an MQTT broker in real-time
- **MQTT Listening**: Receives and processes commands from MQTT for device control
- **Home Assistant Auto-Discovery**: Automatically configures devices in Home Assistant

## Home Assistant Integration

With Home Assistant auto-discovery enabled, the following entities are available:

- [MQTT climate (HVAC)](https://www.home-assistant.io/integrations/climate.mqtt/)
- Temperature readings from the thermostats
- Target temperature settings for each thermostat
- Humidity levels measured by the thermostats
- Current operating state (idle, heating, or cooling)
- Central heating system mode for the apartment building
- Outdoor temperature (opt-in via `OutdoorWeatherEnabled`)
- Outdoor humidity (opt-in via `OutdoorWeatherEnabled`)


## Installation

### Using Docker (Recommended)

The easiest way to run SBM Fizikus to MQTT is using Docker.

**Prerequisites:**
- Docker and Docker Compose
- MQTT Broker (e.g., Mosquitto)
- SBM Fizikus account

**Setup:**
1. Create a `docker-compose.yml` file (see example below)
2. Run `docker-compose up -d`

## Configuration

The application is configured using environment variables. Below is an example `docker-compose.yml` with all available options.

```yaml
services:
  sbm-integration:
    image: ghcr.io/davidsarkany/sbm-fizikus-to-mqtt:1
    container_name: sbm-integration
    restart: unless-stopped
    environment:
      - TZ=Europe/Budapest
      # SBM Fizikus Credentials
      - SbmConnector__Username=your_sbm_username
      - SbmConnector__Password=your_sbm_password
      - SbmConnector__OutdoorWeatherEnabled=false # optional, enables outdoor temp/humidity
      
      # MQTT Broker Configuration
      - MqttConnector__MqttServer__Host=mqtt.example.com
      - MqttConnector__MqttServer__Port=1883
      - MqttConnector__MqttServer__Username=mqtt_user
      - MqttConnector__MqttServer__Password=mqtt_password
      - MqttConnector__MqttServer__ClientId=sbm-fizikus-mqtt # optional
      
      # Publisher & Discovery Configuration
      - PublisherConfiguration__SbmTopic=sbm_fizikus # optional
      - PublisherConfiguration__HomeAssistantTopic=homeassistant # optional
      - PublisherConfiguration__ApartmentSystemModeDiscoveryEnabled=false # optional
      - PublisherConfiguration__ApartmentOutdoorTemperatureDiscoveryEnabled=false # optional
      - PublisherConfiguration__ApartmentOutdoorHumidityDiscoveryEnabled=false # optional
      - PublisherConfiguration__ThermostatTemperatureDiscoveryEnabled=false # optional
      - PublisherConfiguration__ThermostatTargetTemperatureDiscoveryEnabled=false # optional
      - PublisherConfiguration__ThermostatHumidityDiscoveryEnabled=false # optional
      - PublisherConfiguration__ThermostatSystemModeDiscoveryEnabled=false # optional
      - PublisherConfiguration__ClimateDiscoveryEnabled=true # optional
```

### Configuration Options

| Environment Variable | Description                                         | Default value | Required |
|----------------------|-----------------------------------------------------|---------------|----------|
| `SbmConnector__Username` | Your SBM Fizikus username                           | -             | x        |
| `SbmConnector__Password` | Your SBM Fizikus password                           | -             | x        |
| `SbmConnector__OutdoorWeatherEnabled` | Enable outdoor temperature & humidity polling       | false             |          |
| `MqttConnector__MqttServer__Host` | MQTT broker hostname                                | -             | x        |
| `MqttConnector__MqttServer__Port` | MQTT broker port (default: 1883)                    | -             | x        |
| `MqttConnector__MqttServer__Username` | MQTT broker username                                | -             | x        |
| `MqttConnector__MqttServer__Password` | MQTT broker password                                | -             | x        |
| `MqttConnector__MqttServer__ClientId` | MQTT client id                                      | sbm-fizikus-mqtt  |          |
| `PublisherConfiguration__SbmTopic` | Base MQTT topic for SBM data                        | sbm_fizikus       |          |
| `PublisherConfiguration__HomeAssistantTopic` | Base MQTT topic for Home Assistant discovery        | homeassistant     |          |
| `PublisherConfiguration__ApartmentSystemModeDiscoveryEnabled` | Enable/disable auto-discovery for specific entities | false             |          |
| `PublisherConfiguration__ApartmentOutdoorTemperatureDiscoveryEnabled` | Enable outdoor temperature sensor in Home Assistant | false             |          |
| `PublisherConfiguration__ApartmentOutdoorHumidityDiscoveryEnabled` | Enable outdoor humidity sensor in Home Assistant    | false             |          |
| `PublisherConfiguration__ThermostatTemperatureDiscoveryEnabled` | Enable/disable auto-discovery for specific entities | false             |          |
| `PublisherConfiguration__ThermostatTargetTemperatureDiscoveryEnabled` | Enable/disable auto-discovery for specific entities | false             |          |
| `PublisherConfiguration__ThermostatHumidityDiscoveryEnabled` | Enable/disable auto-discovery for specific entities | false             |          |
| `PublisherConfiguration__ThermostatSystemModeDiscoveryEnabled` | Enable/disable auto-discovery for specific entities | false             |          |
| `PublisherConfiguration__ClimateDiscoveryEnabled` | Enable/disable auto-discovery for specific entities | true              |          |

### Manual Installation (Development)

If you want to run the application locally without Docker:

1. .NET 10.0 SDK or later required
2. Clone the repository
3. Restore dependencies: `dotnet restore`
4. Edit `appsettings.json` in `SbmFizikusToMqtt.Web/`
5. Run: `dotnet run --project SbmFizikusToMqtt.Web`

## Usage

### Running with Docker

```bash
docker-compose up -d
```

## Technology Stack

- **.NET**: 10.0
- **MQTT**: MQTTnet for message broker communication
- **Job Scheduling**: `PeriodicTimer`-based hosted services for background jobs
- **Testing**: XUnit with comprehensive test coverage

## Project Structure

```
├── SbmFizikusToMqtt.Web/                        # ASP.NET Core web application entry point
├── SbmFizikusToMqtt.Application/                # Application logic and background jobs
│   └── BackgroundJobs/                          # MQTT listeners and long-running tasks
├── SbmFizikusToMqtt.Domain/                     # Core domain models (Apartment, Thermostat)
├── SbmFizikusToMqtt.SbmConnector/               # SBM Fizikus API integration
├── SbmFizikusToMqtt.MqttConnector/              # MQTT broker communication
├── SbmFizikusToMqtt.MqttConnector.Domain/       # MQTT domain models
├── SbmFizikusToMqtt.HomeAssistantAutoDiscovery/ # Home Assistant integration
└── *.Tests/                                     # Unit and integration tests
```

### Data Flow

1. **Polling**: The application polls SBM Fizikus for flat and thermostat data
2. **Publishing**: Data is published to MQTT topics under the configured base topic
3. **Listening**: The application listens to `/devices/+/set` topics for control commands
4. **Home Assistant**: Devices are auto-discovered in Home Assistant via MQTT discovery
5. **Resilience**: If the MQTT connection drops, the application shuts down and the container restart policy (`restart: unless-stopped`) automatically restarts it, re-establishing all subscriptions and re-publishing discovery messages

### MQTT Topics

**Published Topics:**
- `{SbmTopic}/bridge/state` - Bridge online/offline state
- `{SbmTopic}/apartment_info` - Apartment-level information (system mode, last update)
- `{SbmTopic}/devices/{id}` - Thermostat data (temperature, humidity, mode per device)

**Home Assistant Auto-Discovery Topics:**
- `{HomeAssistantTopic}/sensor/sbm_fizikus-apartment-info/system_mode/config` - Apartment system mode sensor
- `{HomeAssistantTopic}/sensor/sbm_fizikus-{id}/{sensor_type}/config` - Thermostat sensors (temperature, humidity, target temperature, system mode)
- `{HomeAssistantTopic}/climate/sbm_fizikus-{id}/config` - Thermostat climate entity

**Subscribed Topics:**
- `{SbmTopic}/devices/+/set` - Command topics for device control

## Testing

Run tests with:

```bash
dotnet test
```

Test projects include:
- `SbmFizikusToMqtt.Application.Tests/`
- `SbmFizikusToMqtt.SbmConnector.Tests/`
- `SbmFizikusToMqtt.MqttConnector.Tests/`
- `SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Tests/`

## Continuous Integration

- A `dotnet format` check runs on pull requests to enforce repository formatting
- Dependabot opens weekly dependency update pull requests for NuGet and GitHub Actions
- Published Docker images are scanned with Trivy for vulnerabilities

## Troubleshooting

### Connection Issues
- Verify SBM Fizikus credentials
- Ensure MQTT broker is running and accessible
- Check network connectivity and firewall rules
- If the MQTT broker restarts, the application will automatically detect the disconnection, shut down, and restart via Docker — no manual intervention is needed

### Missing Data
- Check MQTT broker for message publication
- Review application logs for errors — SBM API failures include the response body from the API in the log message

### Home Assistant Not Discovering Devices
- Enable discovery flags
- Verify MQTT integration is configured in Home Assistant
- Check Home Assistant logs for discovery messages
- Discovery messages are re-published on every application restart, so restarting the container will re-register all entities