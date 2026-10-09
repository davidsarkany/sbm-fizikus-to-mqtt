using SbmFizikusToMqtt.Application.Services;

namespace SbmFizikusToMqtt.Application.Tests.Services;

public sealed class AutoDiscoveryPublicationStateTests
{
    private readonly AutoDiscoveryPublicationState _sut = new();

    [Fact]
    public void HasPublishedDiscovery_Initially_IsFalse()
    {
        // Act & Assert
        Assert.False(_sut.HasPublishedDiscovery);
    }

    [Fact]
    public void MarkDiscoveryPublished_SetsHasPublishedDiscoveryTrue()
    {
        // Act
        _sut.MarkDiscoveryPublished();

        // Assert
        Assert.True(_sut.HasPublishedDiscovery);
    }

    [Fact]
    public void MarkDiscoveryPublished_WhenCalledTwice_RemainsTrue()
    {
        // Act
        _sut.MarkDiscoveryPublished();
        _sut.MarkDiscoveryPublished();

        // Assert
        Assert.True(_sut.HasPublishedDiscovery);
    }
}