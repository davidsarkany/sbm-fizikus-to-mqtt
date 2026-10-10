# Native AOT: the bridge is a self-contained executable, so the image needs no .NET runtime.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS base
# Time zone data, so TZ (e.g. Europe/Budapest) sets the local time of the log timestamps.
RUN apk add --no-cache tzdata
USER $APP_UID
WORKDIR /app
EXPOSE 8080
# Ready = MQTT connected and SBM polled within three polling intervals (busybox wget ships with Alpine).
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1:8080/health/ready || exit 1

# The -aot SDK image contains the native toolchain (clang, linker) that Native AOT compilation needs.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Directory.Build.props", "src/Directory.Packages.props", "./"]
COPY ["src/SbmFizikusToMqtt.Core/SbmFizikusToMqtt.Core.csproj", "SbmFizikusToMqtt.Core/"]
COPY ["src/SbmFizikusToMqtt.Infrastructure/SbmFizikusToMqtt.Infrastructure.csproj", "SbmFizikusToMqtt.Infrastructure/"]
COPY ["src/SbmFizikusToMqtt.ServiceDefaults/SbmFizikusToMqtt.ServiceDefaults.csproj", "SbmFizikusToMqtt.ServiceDefaults/"]
COPY ["src/SbmFizikusToMqtt.Host/SbmFizikusToMqtt.Host.csproj", "SbmFizikusToMqtt.Host/"]
RUN dotnet restore "SbmFizikusToMqtt.Host/SbmFizikusToMqtt.Host.csproj"
COPY ["src/SbmFizikusToMqtt.Core", "SbmFizikusToMqtt.Core/"]
COPY ["src/SbmFizikusToMqtt.Infrastructure", "SbmFizikusToMqtt.Infrastructure/"]
COPY ["src/SbmFizikusToMqtt.ServiceDefaults", "SbmFizikusToMqtt.ServiceDefaults/"]
COPY ["src/SbmFizikusToMqtt.Host", "SbmFizikusToMqtt.Host/"]
# Trim and AOT warnings fail the publish (TreatWarningsAsErrors), so an unsafe dependency cannot slip in. No debug
# symbols in the image: AOT stack traces still show method names.
RUN dotnet publish "SbmFizikusToMqtt.Host/SbmFizikusToMqtt.Host.csproj" -c "$BUILD_CONFIGURATION" \
    -o /app/publish --no-restore -p:DebugType=none -p:DebugSymbols=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./SbmFizikusToMqtt"]
