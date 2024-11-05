using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DotLocz.Abstractions;
using DotLocz.Services;
using Moq;

namespace DotLocz.UnitTests;

public class LoczServiceTests
{
    private MockFileSystem _mockFileSystem = null!;
    private Mock<ICsvReader> _mockCsvReader = null!;
    private LoczService _loczService = null!;

    private void Init()
    {
        _mockFileSystem = new MockFileSystem();
        _mockCsvReader = new Mock<ICsvReader>();
        _loczService = new LoczService(_mockFileSystem, _mockCsvReader.Object);
    }

    [Fact]
    public async Task GetProjectLocFilesAsync_ShouldReturnCsprojAndLocCsvFiles_WhenTheyExist()
    {
        Init();

        // Arrange: Define the mock directory and file paths
        var directory = "/mockDirectory";

        var mockCsprojPath = _mockFileSystem.Path.Combine(directory, "mockProject.csproj");
        var mockCsvPath1 = _mockFileSystem.Path.Combine(directory, "mockResources1.loc.csv");
        var mockCsvPath2 = _mockFileSystem.Path.Combine(directory, "folder2", "mockResources2.loc.Csv");
        var mockCsvPath3 = _mockFileSystem.Path.Combine(directory, "folder3", "mockResources3.Loc.csv");

        // Create mock files and directories
        _mockFileSystem.Directory.CreateDirectory(directory);
        _mockFileSystem.AddFile(mockCsprojPath, new MockFileData(string.Empty));
        _mockFileSystem.AddFile(mockCsvPath1, new MockFileData(string.Empty));
        _mockFileSystem.Directory.CreateDirectory(_mockFileSystem.Path.GetDirectoryName(mockCsvPath2)!);
        _mockFileSystem.AddFile(mockCsvPath2, new MockFileData(string.Empty));
        _mockFileSystem.Directory.CreateDirectory(_mockFileSystem.Path.GetDirectoryName(mockCsvPath3)!);
        _mockFileSystem.AddFile(mockCsvPath3, new MockFileData(string.Empty));

        // Act: Call GetProjectLocFilesAsync to retrieve project and CSV files
        var result = await _loczService.GetProjectLocFilesAsync(directory);

        // Assert: Verify that the result contains one project with its associated CSV files
        Assert.Single(result);
        Assert.Equal(mockCsprojPath, result.Keys.First());
        Assert.Equal(3, result.Values.First().Length);
        Assert.Contains(mockCsvPath1, result.Values.First());
        Assert.Contains(mockCsvPath2, result.Values.First());
        Assert.Contains(mockCsvPath3, result.Values.First());
    }


    [Fact]
    public void ShouldGenerate_ReturnsTrue_WhenCsvIsNewerThanEnumFile()
    {
        Init();

        // Arrange
        var outputPath = "/mockDirectory/Output";
        _mockFileSystem.Directory.CreateDirectory(outputPath);

        var csvPath = _mockFileSystem.Path.Combine(outputPath, "test.loc.csv");
        var enumPath = _mockFileSystem.Path.Combine(outputPath, "test.cs");

        _mockFileSystem.AddFile(csvPath, new MockFileData("mock content") { LastWriteTime = DateTime.Now });
        _mockFileSystem.AddFile(enumPath, new MockFileData("mock enum content") { LastWriteTime = DateTime.Now.AddMinutes(-10) });

        // Act
        var result = _loczService.ShouldGenerate(csvPath, outputPath);

        // Assert
        Assert.True(result, "ShouldGenerate should return true if CSV is newer than the enum file.");
    }

    [Fact]
    public void ShouldGenerate_ReturnsTrue_WhenEnumFileDoesNotExist()
    {
        Init();

        // Arrange
        var outputPath = "/mockDirectory/Output";
        _mockFileSystem.Directory.CreateDirectory(outputPath);

        var csvPath = _mockFileSystem.Path.Combine(outputPath, "test.loc.csv");
        _mockFileSystem.AddFile(csvPath, new MockFileData("mock content"));

        // Act
        var result = _loczService.ShouldGenerate(csvPath, outputPath);

        // Assert
        Assert.True(result, "ShouldGenerate should return true if the enum file does not exist.");
    }

    [Fact]
    public void ShouldGenerate_ReturnsFalse_WhenEnumIsNewerThanCsv()
    {
        Init();

        // Arrange
        var outputPath = "/mockDirectory/Output";
        _mockFileSystem.Directory.CreateDirectory(outputPath);

        var csvPath = _mockFileSystem.Path.Combine(outputPath, "test.loc.csv");
        var enumPath = _mockFileSystem.Path.Combine(outputPath, "test.cs");

        _mockFileSystem.AddFile(csvPath, new MockFileData("mock content") { LastWriteTime = DateTime.Now.AddMinutes(-10) });
        _mockFileSystem.AddFile(enumPath, new MockFileData("mock enum content") { LastWriteTime = DateTime.Now });

        // Act
        var result = _loczService.ShouldGenerate(csvPath, outputPath);

        // Assert
        Assert.False(result, "ShouldGenerate should return false if the enum file is newer than the CSV.");
    }

    [Fact]
    public async Task GenerateLocFileResourcesAsync_ShouldGenerateEnumAndResxFiles()
    {
        Init();

        // Arrange
        var locFilePath = "/mockDirectory/test.loc.csv";
        var outputPath = "/mockDirectory/Output";
        var namespaceName = "MockNamespace";
        _mockFileSystem.Directory.CreateDirectory(outputPath);

        // Sample CSV content with headers and rows
        _mockFileSystem.AddFile(locFilePath, new MockFileData("Key,en-US,fr-FR\nHello,Hello,Bonjour\nGoodbye,Goodbye,Au revoir"));

        // Setup first call to ReadRow to return the header row
        int row = 0;
        string[][] csvRows = [["Key", "en-US", "fr-FR"], ["Hello", "Hello", "Bonjour"], ["Goodbye", "Goodbye", "Au revoir"]];
        _mockCsvReader.Setup(m => m.ReadRow(out It.Ref<string[]>.IsAny))
            .Returns((out string[] columns) =>
            {
                columns = [];
                if (row < 3)
                {
                    columns = csvRows[row];
                    row++;
                    return true;
                }
                return false;
            });

        // Act
        await _loczService.GenerateLocFileResourcesAsync(locFilePath, namespaceName, outputPath);

        // Assert
        var enumPath = _mockFileSystem.Path.Combine(outputPath, "test.cs");
        var resxPathEn = _mockFileSystem.Path.Combine(outputPath, "test.en-US.resx");
        var resxPathFr = _mockFileSystem.Path.Combine(outputPath, "test.fr-FR.resx");

        Assert.True(_mockFileSystem.File.Exists(enumPath), "Enum file should be created.");
        Assert.True(_mockFileSystem.File.Exists(resxPathEn), "English RESX file should be created.");
        Assert.True(_mockFileSystem.File.Exists(resxPathFr), "French RESX file should be created.");

        // Verify content generation by checking that the enum content contains expected values
        var enumContent = _mockFileSystem.File.ReadAllText(enumPath);
        Assert.Contains("public enum test", enumContent);
        Assert.Contains("Hello,", enumContent);
        Assert.Contains("Goodbye,", enumContent);

        // Verify that RESX files contain expected values
        var resxContentEn = _mockFileSystem.File.ReadAllText(resxPathEn);
        Assert.Contains("<data name=\"Hello\"", resxContentEn);
        Assert.Contains("<value>Hello</value>", resxContentEn);

        var resxContentFr = _mockFileSystem.File.ReadAllText(resxPathFr);
        Assert.Contains("<data name=\"Goodbye\"", resxContentFr);
        Assert.Contains("<value>Au revoir</value>", resxContentFr);
    }


    [Fact]
    public async Task GenerateExtensionsClassAsync_ShouldCreateExtensionsFile_WhenItDoesNotExist()
    {
        Init();

        // Arrange
        var outputPath = "/mockDirectory/Output";
        var namespaceName = "MockNamespace";
        var extensionsPath = _mockFileSystem.Path.Combine(outputPath, "LoczExtensions.cs");
        _mockFileSystem.Directory.CreateDirectory(outputPath);

        // Act
        await _loczService.GenerateExtensionsClassAsync(namespaceName, outputPath);

        // Assert
        Assert.True(_mockFileSystem.File.Exists(extensionsPath), "Extensions file should be created.");

        var content = _mockFileSystem.File.ReadAllText(extensionsPath);
        Assert.Contains("public static class LoczExtensions", content);
        Assert.Contains("IStringLocalizer localizer", content);
    }

    [Fact]
    public async Task GenerateExtensionsClassAsync_ShouldNotCreateExtensionsFile_WhenItAlreadyExists()
    {
        Init();

        // Arrange
        var outputPath = "/mockDirectory/Output";
        var namespaceName = "MockNamespace";
        var extensionsPath = _mockFileSystem.Path.Combine(outputPath, "LoczExtensions.cs");
        _mockFileSystem.Directory.CreateDirectory(outputPath);
        _mockFileSystem.AddFile(extensionsPath, new MockFileData("Existing content"));

        // Act
        await _loczService.GenerateExtensionsClassAsync(namespaceName, outputPath);

        // Assert
        Assert.True(_mockFileSystem.File.Exists(extensionsPath), "Extensions file should already exist.");
        var content = _mockFileSystem.File.ReadAllText(extensionsPath);
        Assert.Equal("Existing content", content); // Ensures the existing file was not overwritten
    }



}