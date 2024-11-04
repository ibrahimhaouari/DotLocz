using System.Text;

namespace DotLocz.Abstractions;

public interface ILoczService
{
    Task ScanAndGenerateAsync(string directory, string relativeOutputPath);
    Task<Dictionary<string, string[]>> GetProjectLocFilesAsync(string directory);
    Task GenerateLocFileResourcesAsync(string locFilePath, string nameSpace, string outputPath);
    Task GenerateExtensionsClassAsync(string nameSpace, string outputPath);
    Task SaveFileAsync(string content, string outputPath);

    bool ShouldGenerate(string csvPath, string enumPath);
}