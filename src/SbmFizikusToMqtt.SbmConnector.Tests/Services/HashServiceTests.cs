using SbmFizikusToMqtt.SbmConnector.Services;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Services;

public class HashServiceTests
{
    [Theory]
    [InlineData("hello", "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("123456", "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92")]
    public void Sha256Hash_KnownValues_ReturnsExpectedHash(string input, string expectedHash)
    {
        // Arrange - input and expectedHash provided as parameters

        // Act
        var actualHash = HashService.Sha256Hash(input);

        // Assert
        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void Sha256Hash_SameInput_ReturnsSameHash()
    {
        // Arrange
        var input = "test";

        // Act
        var hash1 = HashService.Sha256Hash(input);
        var hash2 = HashService.Sha256Hash(input);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Sha256Hash_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => HashService.Sha256Hash(input!));
    }
}