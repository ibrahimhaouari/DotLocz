
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using CsvHelper;

namespace DotLocz;

public sealed class LoczService(IFileSystem fileSystem) : ILoczService
{
    private readonly IFileSystem fileSystem = fileSystem;

    public async Task ScanAndGenerateAsync(string directory, string relativeOutputPath)
    {
        Console.WriteLine($"[{DateTime.Now}] Starting localization scan in directory: {directory}");

        var projectLocs = await GetProjectLocFilesAsync(directory);
        if (projectLocs.Count == 0)
        {
            return;
        }

        foreach (var (projectPath, csvPaths) in projectLocs)
        {
            if (csvPaths.Length == 0)
            {
                continue;
            }

            var outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, relativeOutputPath);
            var nameSpace = $"{Path.GetFileNameWithoutExtension(projectPath)}.{relativeOutputPath.Replace("/", ".")}";
            Directory.CreateDirectory(outputPath);

            foreach (var csvPath in csvPaths)
            {
                if (ShouldGenerate(csvPath, outputPath))
                {
                    await GenerateLocFileResourcesAsync(csvPath, nameSpace, outputPath);
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] Skipping {csvPath} - up to date");
                }
            }

            await GenerateExtensionsClassAsync(nameSpace, outputPath);
        }
    }

    public bool ShouldGenerate(string csvPath, string outputPath)
    {
        var resourceName = Path.GetFileNameWithoutExtension(csvPath).Replace(".loc", "");
        var enumPath = Path.Combine(outputPath, $"{resourceName}.cs");

        return !File.Exists(enumPath) || File.GetLastWriteTime(csvPath) > File.GetLastWriteTime(enumPath);
    }

    public async Task<Dictionary<string, string[]>> GetProjectLocFilesAsync(string directory)
    {
        // Find all .csproj files to determine project locations
        var projectPaths = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);
        if (projectPaths.Length == 0)
        {
            Console.WriteLine($"[{DateTime.Now}] No .csproj files found in the directory: {directory}");
            return [];
        }

        var projectLocs = new Dictionary<string, string[]>();
        foreach (var projectPath in projectPaths)
        {
            // Find all .loc.csv files in each project directory
            var csvPaths = Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*.loc.csv", SearchOption.AllDirectories);

            if (csvPaths.Length > 0)
            {
                Console.WriteLine($"[{DateTime.Now}] Found {csvPaths.Length} CSV files in project: {Path.GetFileName(projectPath)}");

                projectLocs.Add(projectPath, csvPaths);
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] No CSV files found in project: {Path.GetFileName(projectPath)}");
            }
        }

        await Task.CompletedTask;
        return projectLocs;
    }


    public async Task GenerateLocFileResourcesAsync(string locFilePath, string nameSpace, string outputPath)
    {
        var resourceName = Path.GetFileNameWithoutExtension(locFilePath).Replace(".loc", "");

        Console.WriteLine($"[{DateTime.Now}] Generating RESX files for {resourceName}...");

        using var reader = new StreamReader(locFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var enumContent = new StringBuilder();
        enumContent.AppendLine($"namespace {nameSpace};");
        enumContent.AppendLine($"public enum {resourceName} {{");

        csv.Read(); // Read header to determine languages
        var langCount = csv.ColumnCount - 1;
        var Languages = new string[langCount];
        var resxContents = new StringBuilder[langCount];

        // Initialize each language's RESX file
        for (var i = 0; i < langCount; i++)
        {
            var lang = csv.GetField(i + 1)!;
            Languages[i] = lang;
            resxContents[i] = new StringBuilder();
            resxContents[i].AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            resxContents[i].AppendLine("<root>");
        }

        // Populate RESX content from CSV rows
        while (csv.Read())
        {
            var key = csv.GetField<string>(0);
            if (!string.IsNullOrEmpty(key))
            {
                enumContent.AppendLine($"    {key},");

                for (var i = 0; i < langCount; i++)
                {
                    var value = csv.GetField(i + 1)!;
                    resxContents[i].AppendLine($"  <data name=\"{key}\" xml:space=\"preserve\">");
                    resxContents[i].AppendLine($"    <value>{value}</value>");
                    resxContents[i].AppendLine("  </data>");
                }
            }
        }

        // Close and write enum content to file
        enumContent.AppendLine("}");
        var enumPath = Path.Combine(outputPath, $"{resourceName}.cs");
        await File.WriteAllTextAsync(enumPath, enumContent.ToString());
        Console.WriteLine($"[{DateTime.Now}] Enum file created: {enumPath}");

        // Close and write RESX content to files
        for (var i = 0; i < langCount; i++)
        {
            resxContents[i].AppendLine("</root>");
            var resxPath = Path.Combine(outputPath, $"{resourceName}.{Languages[i]}.resx");
            await File.WriteAllTextAsync(resxPath, resxContents[i].ToString());
            Console.WriteLine($"[{DateTime.Now}] RESX file created: {resxPath}");
        }
    }

    public async Task GenerateExtensionsClassAsync(string nameSpace, string outputPath)
    {
        var extensionsPath = Path.Combine(outputPath, "LoczExtensions.cs");
        if (File.Exists(extensionsPath))
        {
            Console.WriteLine($"[{DateTime.Now}] Extensions file already exists: {extensionsPath}");
            return;
        }

        Console.WriteLine($"[{DateTime.Now}] Generating extensions file...");

        var content = $$"""
            using Microsoft.Extensions.Localization;

            namespace {{nameSpace}};

            public static class LoczExtensions
            {
                public static string Get(this IStringLocalizer localizer, Enum key, params object[] args) =>
                    localizer.Get(key.ToString(), args);

                public static string Get(this IStringLocalizer localizer, string key, params object[] args) =>
                    localizer[key, args];
            }
            """;

        await SaveFileAsync(content, extensionsPath);
        Console.WriteLine($"[{DateTime.Now}] Extensions file created: {extensionsPath}");
        return;
    }

    public async Task SaveFileAsync(string content, string outputPath)
    {
        await fileSystem.File.WriteAllTextAsync(outputPath, content);
    }
}
