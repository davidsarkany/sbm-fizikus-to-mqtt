using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Extensions;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Extensions;

public class IServiceCollectionExtensionTests
{
    [Fact]
    public void ConfigureSbmConnector_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add required configuration for HttpClient setup
        var config = new SbmConfiguration
        {
            BaseUrl = "https://test.example.com",
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        var result = services.ConfigureSbmConnector();

        // Assert
        Assert.Same(services, result); // Should return the same collection for fluent API

        var serviceProvider = services.BuildServiceProvider();

        // Verify TimeProvider is registered
        var timeProvider = serviceProvider.GetService<TimeProvider>();
        Assert.NotNull(timeProvider);
        Assert.Same(TimeProvider.System, timeProvider);

        // Verify ITokenService is registered as singleton
        var tokenService1 = serviceProvider.GetService<ITokenService>();
        var tokenService2 = serviceProvider.GetService<ITokenService>();
        Assert.NotNull(tokenService1);
        Assert.Same(tokenService1, tokenService2); // Should be the same instance (singleton)

        // Verify ISbmService is registered as singleton
        var sbmService1 = serviceProvider.GetService<ISbmService>();
        var sbmService2 = serviceProvider.GetService<ISbmService>();
        Assert.NotNull(sbmService1);
        Assert.Same(sbmService1, sbmService2); // Should be the same instance (singleton)

        // Verify IApartmentService is registered as singleton
        var apartmentService1 = serviceProvider.GetService<IApartmentService>();
        var apartmentService2 = serviceProvider.GetService<IApartmentService>();
        Assert.NotNull(apartmentService1);
        Assert.Same(apartmentService1, apartmentService2); // Should be the same instance (singleton)
    }

    [Fact]
    public void ConfigureSbmConnector_ShouldRegisterHttpClientWithCorrectConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var testBaseUrl = "https://api.example.com";

        var config = new SbmConfiguration
        {
            BaseUrl = testBaseUrl,
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        services.ConfigureSbmConnector();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("SbmClient");

        Assert.NotNull(httpClient);
        Assert.Equal(new Uri(testBaseUrl), httpClient.BaseAddress);
        Assert.Contains("application/json", httpClient.DefaultRequestHeaders.Accept.ToString());
    }

    [Fact]
    public void ConfigureSbmConnector_WithMissingConfiguration_ShouldThrowWhenCreatingHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Don't add configuration - this should cause an error when HttpClient is created
        services.ConfigureSbmConnector();

        // Act & Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        Assert.Throws<ArgumentNullException>(() => httpClientFactory.CreateClient("SbmClient"));
    }

    [Fact]
    public void ConfigureSbmConnector_ShouldAllowMultipleRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        var config = new SbmConfiguration
        {
            BaseUrl = "https://test.example.com",
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act - Call the extension method multiple times
        services.ConfigureSbmConnector();
        services.ConfigureSbmConnector();

        // Assert - Should not throw and services should still be resolvable
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<ITokenService>());
        Assert.NotNull(serviceProvider.GetService<ISbmService>());
        Assert.NotNull(serviceProvider.GetService<IApartmentService>());
        Assert.NotNull(serviceProvider.GetService<TimeProvider>());
    }

    [Theory]
    [InlineData("https://api.test.com")]
    [InlineData("https://another.api.com")]
    [InlineData("http://localhost:8080")]
    public void ConfigureSbmConnector_WithDifferentBaseUrls_ShouldConfigureHttpClientCorrectly(string baseUrl)
    {
        // Arrange
        var services = new ServiceCollection();

        var config = new SbmConfiguration
        {
            BaseUrl = baseUrl,
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        services.ConfigureSbmConnector();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("SbmClient");

        Assert.Equal(new Uri(baseUrl), httpClient.BaseAddress);
    }

    [Fact]
    public void ConfigureSbmConnector_ShouldRegisterCorrectServiceLifetimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new SbmConfiguration
        {
            BaseUrl = "https://test.example.com",
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        services.ConfigureSbmConnector();

        // Assert - Check service descriptors for correct lifetimes
        var serviceDescriptors = services.ToArray();

        var timeProviderDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(TimeProvider));
        Assert.NotNull(timeProviderDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, timeProviderDescriptor.Lifetime);

        var tokenServiceDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(ITokenService));
        Assert.NotNull(tokenServiceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, tokenServiceDescriptor.Lifetime);

        var sbmServiceDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(ISbmService));
        Assert.NotNull(sbmServiceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, sbmServiceDescriptor.Lifetime);

        var apartmentServiceDescriptor =
            serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(IApartmentService));
        Assert.NotNull(apartmentServiceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, apartmentServiceDescriptor.Lifetime);
    }

    [Fact]
    public void ConfigureSbmConnector_ShouldRegisterNamedHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new SbmConfiguration
        {
            BaseUrl = "https://test.example.com",
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        services.ConfigureSbmConnector();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Test that named client is different from default
        var namedClient = httpClientFactory.CreateClient("SbmClient");
        var defaultClient = httpClientFactory.CreateClient();

        Assert.NotNull(namedClient);
        Assert.NotNull(defaultClient);
        Assert.NotEqual(namedClient.BaseAddress, defaultClient.BaseAddress);
    }

    [Fact]
    public void ConfigureSbmConnector_HttpClientShouldHaveAcceptHeader()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new SbmConfiguration
        {
            BaseUrl = "https://test.example.com",
            Username = "testuser",
            Password = "testpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        services.ConfigureSbmConnector();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("SbmClient");

        var acceptHeader = httpClient.DefaultRequestHeaders.Accept.FirstOrDefault();
        Assert.NotNull(acceptHeader);
        Assert.Equal("application/json", acceptHeader.MediaType);
    }

    [Fact]
    public void ConfigureSbmConnector_ShouldNotThrowWithValidConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new SbmConfiguration
        {
            BaseUrl = "https://valid.example.com",
            Username = "user",
            Password = "pass"
        };
        services.AddSingleton(Options.Create(config));

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => services.ConfigureSbmConnector());
        Assert.Null(exception);

        // Additional verification that all services can be resolved
        var serviceProvider = services.BuildServiceProvider();
        var allServicesResolvable = Record.Exception(() =>
        {
            serviceProvider.GetRequiredService<ITokenService>();
            serviceProvider.GetRequiredService<ISbmService>();
            serviceProvider.GetRequiredService<IApartmentService>();
            serviceProvider.GetRequiredService<TimeProvider>();
            serviceProvider.GetRequiredService<IHttpClientFactory>();
        });

        Assert.Null(allServicesResolvable);
    }

    [Fact]
    public void ConfigureSbmConnector_ConfigurationIsUsedByHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new SbmConfiguration
        {
            BaseUrl = "https://config-test.example.com",
            Username = "configuser",
            Password = "configpass"
        };
        services.AddSingleton(Options.Create(config));

        // Act
        services.ConfigureSbmConnector();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("SbmClient");

        // Verify that the configuration was actually used
        Assert.Equal(config.BaseUrl, httpClient.BaseAddress?.ToString().TrimEnd('/'));
    }
}