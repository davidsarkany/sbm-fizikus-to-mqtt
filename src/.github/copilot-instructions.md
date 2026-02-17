# GitHub Copilot Instructions for SBM Fizikus to MQTT

## Project Overview

This is a **.NET 10.0** C# solution that bridges SBM Fizikus building management systems with MQTT brokers. The solution handles apartment climate control data, thermostat information, and enables remote temperature adjustments.

---

## Code Generation Guidelines

### General C# Conventions

- **Target Framework:** .NET 10.0
- **Language Version:** Latest C# features enabled
- **Nullable Reference Types:** Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings:** Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)

### Record and Class Patterns

When generating domain models, use **sealed records** with required properties:

```csharp
public sealed record ModelName
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset LastUpdate { get; init; }
}
```

For DTOs (Request/Response models), use `[SetsRequiredMembers]` with constructors:

```csharp
public sealed record RequestDto
{
    [SetsRequiredMembers]
    public RequestDto(string value)
    {
        Value = value;
    }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
```

### Service Implementation Patterns

- Use **primary constructors** for dependency injection
- Mark implementation classes as `internal sealed`
- Use `CancellationToken` for async operations

```csharp
internal sealed class MyService(IDependency dependency) : IMyService
{
    public async Task<Result> DoWorkAsync(CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### Dependency Injection

- Register services as **Singletons** (this project uses singleton pattern)
- Use named `HttpClient` instances via `IHttpClientFactory`
- Configuration via `IOptions<T>` pattern

---

## Testing Standards

### Test Framework
- **xUnit** for test framework
- **Moq** for mocking
- **Bogus** for fake data generation

### Test Structure (REQUIRED)

All tests MUST follow AAA pattern with comments:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var sut = new SystemUnderTest();
    var input = "test";

    // Act
    var result = sut.Method(input);

    // Assert
    Assert.Equal(expected, result);
}
```

### Test Naming Convention

`MethodName_Scenario_ExpectedResult`

Examples:
- `GetToken_ValidCredentials_ReturnsToken`
- `GetApartmentInfo_NoBuildingAccess_ThrowsSbmException`
- `ChangeTemperature_InvalidThermostatId_ThrowsException`

### Mocking Pattern

```csharp
[Fact]
public async Task ServiceMethod_WithDependency_Works()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    mockDependency.Setup(x => x.Method(It.IsAny<string>()))
        .ReturnsAsync(expectedValue);
    
    var sut = new MyService(mockDependency.Object);

    // Act
    var result = await sut.DoWork();

    // Assert
    Assert.NotNull(result);
    mockDependency.Verify(x => x.Method(It.IsAny<string>()), Times.Once);
}
```

### Bogus for Test Data

```csharp
private static readonly Faker<MyModel> _faker = new Faker<MyModel>()
    .RuleFor(x => x.Id, f => f.Random.Int(1, 1000))
    .RuleFor(x => x.Name, f => f.Name.FullName())
    .RuleFor(x => x.Temperature, f => f.Random.Double(15, 30));
```

---

## Project Structure Guidelines

### Solution Architecture

```
SbmFizikusToMqtt/
├── SbmFizikusToMqtt.Domain/                    # Core domain models
├── SbmFizikusToMqtt.SbmConnector/              # SBM API communication
├── SbmFizikusToMqtt.SbmConnector.Tests/        # SBM connector tests
├── SbmFizikusToMqtt.MqttConnector/             # MQTT broker communication
├── SbmFizikusToMqtt.MqttConnector.Domain/      # MQTT domain models
├── SbmFizikusToMqtt.MqttConnector.Tests/       # MQTT connector tests
├── SbmFizikusToMqtt.HomeAssistantAutoDiscovery/    # Home Assistant integration
├── SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Tests/
├── SbmFizikusToMqtt.Application/               # Application layer (background jobs)
├── SbmFizikusToMqtt.Application.Tests/         # Application layer tests
└── SbmFizikusToMqtt.Web/                       # ASP.NET Core Web host
```

### Adding New Features

1. **Domain Models** → `SbmFizikusToMqtt.Domain/` or `SbmFizikusToMqtt.MqttConnector.Domain/`
   - Pure records with no dependencies
   
2. **Service Interfaces** → `{Project}/Interfaces/`
   - Define contracts with `I` prefix
   
3. **Service Implementations** → `{Project}/Services/`
   - Internal sealed classes
   
4. **Request/Response DTOs** → `{Project}/Models/`
   - Requests/ and Response/ subfolders where applicable
   
5. **Strategies** → `SbmFizikusToMqtt.HomeAssistantAutoDiscovery/Strategies/`
   - Use strategy pattern for Home Assistant discovery
   
6. **Background Jobs** → `SbmFizikusToMqtt.Application/BackgroundJobs/`
   - Long-running hosted services
   
7. **Unit Tests** → `{Project}.Tests/`
   - Mirror the main project structure

### JSON Serialization

Use `System.Text.Json` with these patterns:

```csharp
[JsonPropertyName("snake_case_name")]
public required string PropertyName { get; init; }
```

Custom converters go in `Converters/` folder.

---

## Common Patterns in This Codebase

### Exception Handling

Use project-specific exceptions:
- `SbmException` - General SBM-related errors
- `SbmInvalidResponseException` - API response parsing failures

```csharp
if (result == null)
    throw new SbmInvalidResponseException("Message describing the issue");
```

### Async/Await

- Always use `async/await` for I/O operations
- Pass `CancellationToken` through the call chain
- Use `ConfigureAwait(false)` in library code if needed

### Extension Methods

Place in `Extensions/` folder with naming convention:
- File: `{TypeExtended}.cs` or `{TypeExtended}Extensions.cs`
- Class: `{TypeExtended}Extension` (singular)

---

## API Client Patterns

### HTTP Requests

```csharp
var requestData = new RequestDto(value);
var content = new StringContent(
    JsonSerializer.Serialize(requestData), 
    Encoding.UTF8, 
    "application/json");
var response = await _httpClient.PutAsync("/endpoint", content);
response.EnsureSuccessStatusCode();
```

### Response Parsing

```csharp
var json = await response.Content.ReadAsStringAsync();
try
{
    var result = JsonSerializer.Deserialize<ResponseDto>(json, _jsonSerializerOptions);
    if (result == null)
        throw new SbmInvalidResponseException("Null response received");
    return result;
}
catch (JsonException ex)
{
    throw new SbmInvalidResponseException($"Parse error: {ex.Message}", ex);
}
```

---

## Configuration

### appsettings.json Structure

```json
{
  "SbmConfiguration": {
    "BaseUrl": "https://api.sbm.example.com",
    "Username": "user",
    "Password": "pass"
  }
}
```

### Options Pattern

```csharp
services.Configure<SbmConfiguration>(configuration.GetSection("SbmConfiguration"));
```

---

## Do NOT

❌ Use `public` for service implementation classes (use `internal`)  
❌ Forget AAA comments in tests  
❌ Use `var` without clear type inference  
❌ Skip `CancellationToken` in async methods  
❌ Use `Newtonsoft.Json` (use `System.Text.Json`)  
❌ Create mutable DTOs (use `init` properties)  
❌ Forget `sealed` modifier on records and implementation classes  

## DO

✅ Use `required` modifier for mandatory properties  
✅ Follow AAA test pattern with comments  
✅ Use primary constructors for DI  
✅ Register services as Singletons  
✅ Use named HttpClient via factory  
✅ Throw project-specific exceptions  
✅ Write both positive and negative test cases  

---

## Quick Reference

| What | Where | Pattern |
|------|-------|---------|
| Domain models | `Domain/` or `MqttConnector.Domain/` | `public sealed record` |
| Interfaces | `{Project}/Interfaces/` | `internal interface` |
| Services | `{Project}/Services/` | `internal sealed class` |
| DTOs | `{Project}/Models/` | `public sealed record` |
| Converters | `{Project}/Converters/` | `JsonConverter<T>` |
| Strategies | `HomeAssistantAutoDiscovery/Strategies/` | Strategy pattern |
| Background Jobs | `Application/BackgroundJobs/` | `BackgroundService` |
| Tests | `{Project}.Tests/` | `[Fact]` with AAA |
| Extensions | `{Project}/Extensions/` | `static class` |
| Configurations | `{Project}/Configurations/` | `public sealed record` |

---

## Development Environment

- **OS:** Windows 11
- **Shell:** PowerShell (use PowerShell syntax for all terminal commands)
- **IDE:** JetBrains Rider

### Terminal Command Guidelines

- Use PowerShell syntax (e.g., `;` to chain commands, not `&&`)
- Use backslash `\` for Windows file paths
- Use `dotnet` CLI for build, test, and run operations

---

## Build Commands

```powershell
# Build solution
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test project
dotnet test .\SbmFizikusToMqtt.SbmConnector.Tests\

# Format code
dotnet format

# Clean and rebuild
dotnet clean ; dotnet build
```

---
