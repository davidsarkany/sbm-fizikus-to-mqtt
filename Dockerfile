FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/SbmFizikusToMqtt.Web/SbmFizikusToMqtt.Web.csproj", "SbmFizikusToMqtt.Web/"]
COPY ["src/SbmFizikusToMqtt.HomeAssistantAutoDiscovery/SbmFizikusToMqtt.HomeAssistantAutoDiscovery.csproj", "SbmFizikusToMqtt.HomeAssistantAutoDiscovery/"]
COPY ["src/SbmFizikusToMqtt.Domain/SbmFizikusToMqtt.Domain.csproj", "SbmFizikusToMqtt.Domain/"]
COPY ["src/SbmFizikusToMqtt.Application/SbmFizikusToMqtt.Application.csproj", "SbmFizikusToMqtt.Application/"]
COPY ["src/SbmFizikusToMqtt.MqttConnector.Domain/SbmFizikusToMqtt.MqttConnector.Domain.csproj", "SbmFizikusToMqtt.MqttConnector.Domain/"]
COPY ["src/SbmFizikusToMqtt.MqttConnector/SbmFizikusToMqtt.MqttConnector.csproj", "SbmFizikusToMqtt.MqttConnector/"]
COPY ["src/SbmFizikusToMqtt.SbmConnector/SbmFizikusToMqtt.SbmConnector.csproj", "SbmFizikusToMqtt.SbmConnector/"]
RUN dotnet restore "SbmFizikusToMqtt.Web/SbmFizikusToMqtt.Web.csproj"
COPY src .
WORKDIR "/src/SbmFizikusToMqtt.Web"
RUN dotnet build "./SbmFizikusToMqtt.Web.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SbmFizikusToMqtt.Web.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SbmFizikusToMqtt.Web.dll"]
