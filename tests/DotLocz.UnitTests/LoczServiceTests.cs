using System.IO.Abstractions;
using Moq;

namespace DotLocz.UnitTests;

public class LoczServiceTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly LoczService _loczService;

    public LoczServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _loczService = new LoczService(_mockFileSystem.Object);
    }

    [Fact]
    public async Task ScanAndGenerateAsync_ShouldScanForCsprojFiles()
    {
        // Arrange
        var mockDirectory = "/mockDirectory";
        _mockFileSystem.Setup(fs => fs.Directory.GetFiles(mockDirectory, "*.csproj", SearchOption.AllDirectories))
                       .Returns(["/mockDirectory/test.csproj"]);

        _mockFileSystem.Setup(fs => fs.Directory.GetFiles("/mockDirectory", "*.loc.csv", SearchOption.AllDirectories))
                       .Returns(["/mockDirectory/test.loc.csv"]);

        // Act
        await _loczService.ScanAndGenerateAsync(mockDirectory, "LoczOutput");

        // Assert
        _mockFileSystem.Verify(fs => fs.Directory.GetFiles(It.IsAny<string>(), "*.csproj", SearchOption.AllDirectories), Times.Once);
        _mockFileSystem.Verify(fs => fs.Directory.GetFiles(It.IsAny<string>(), "*.loc.csv", SearchOption.AllDirectories), Times.Once);
    }
}