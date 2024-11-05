using System.IO.Abstractions;
using System.Text;
using DotLocz.Abstractions;

namespace DotLocz.Services;

public sealed class LoczService(IFileSystem fileSystem, ICsvReader csvReader) : ILoczService
{
    private readonly IFileSystem fileSystem = fileSystem;
    private readonly ICsvReader csvReader = csvReader;

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
            fileSystem.Directory.CreateDirectory(outputPath);

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
        var resourceName = Path.GetFileNameWithoutExtension(csvPath).Replace(".loc", "", StringComparison.OrdinalIgnoreCase);
        var enumPath = Path.Combine(outputPath, $"{resourceName}.cs");

        return !fileSystem.File.Exists(enumPath) || fileSystem.File.GetLastWriteTime(csvPath) > fileSystem.File.GetLastWriteTime(enumPath);
    }

    public async Task<Dictionary<string, string[]>> GetProjectLocFilesAsync(string directory)
    {
        // Find all .csproj files to determine project locations
        var projectPaths = FindFiles(directory, ".csproj").ToArray();
        if (projectPaths.Length == 0)
        {
            Console.WriteLine($"[{DateTime.Now}] No .csproj files found in the directory: {directory}");
            return [];
        }

        var projectLocs = new Dictionary<string, string[]>();
        foreach (var projectPath in projectPaths)
        {
            // Find all .loc.csv files in each project directory
            var csvPaths = FindFiles(Path.GetDirectoryName(projectPath)!, ".loc.csv").ToArray();

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

    private IEnumerable<string> FindFiles(string directory, string extension)
    {
        foreach (var file in fileSystem.Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(file).EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                yield return file;
        }

        foreach (var subDirectory in fileSystem.Directory.GetDirectories(directory))
        {
            foreach (var file in FindFiles(subDirectory, extension))
                yield return file;
        }
    }


    public async Task GenerateLocFileResourcesAsync(string locFilePath, string nameSpace, string outputPath)
    {
        var resourceName = Path.GetFileNameWithoutExtension(locFilePath).Replace(".loc", "", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"[{DateTime.Now}] Generating RESX files for {locFilePath}...");

        csvReader.Init(locFilePath);

        var enumContent = new StringBuilder();
        enumContent.AppendLine($"namespace {nameSpace};");
        enumContent.AppendLine($"public enum {resourceName} {{");


        if (!csvReader.ReadRow(out var firstRow))
        {
            Console.WriteLine($"[{DateTime.Now}] No rows found in CSV file: {locFilePath}");
            return;
        }

        var langCount = firstRow.Length - 1;
        var Languages = new string[langCount];
        var resxContents = new StringBuilder[langCount];

        // Initialize each language's RESX file
        for (var i = 0; i < langCount; i++)
        {
            var lang = firstRow[i + 1];
            Languages[i] = lang;
            resxContents[i] = new StringBuilder();
            resxContents[i].AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            resxContents[i].AppendLine("<root>");
        }

        // Populate RESX content from CSV rows
        while (csvReader.ReadRow(out var row))
        {
            var key = row[0];
            if (!string.IsNullOrEmpty(key))
            {
                enumContent.AppendLine($"    {key},");

                for (var i = 0; i < langCount; i++)
                {
                    var value = new StringBuilder(row[i + 1]);
                    // Escape special characters
                    value = value
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");

                    resxContents[i].AppendLine($"  <data name=\"{key}\" xml:space=\"preserve\">");
                    resxContents[i].AppendLine($"    <value>{value}</value>");
                    resxContents[i].AppendLine("  </data>");
                }
            }
        }

        // Close and write enum content to file
        enumContent.AppendLine("}");
        var enumPath = Path.Combine(outputPath, $"{resourceName}.cs");
        await SaveFileAsync(enumContent.ToString(), enumPath);
        Console.WriteLine($"[{DateTime.Now}] Enum file created: {enumPath}");

        // Close and write RESX content to files
        for (var i = 0; i < langCount; i++)
        {
            resxContents[i].AppendLine("</root>");
            var resxPath = Path.Combine(outputPath, $"{resourceName}.{Languages[i]}.resx");
            await SaveFileAsync(resxContents[i].ToString(), resxPath);
            Console.WriteLine($"[{DateTime.Now}] RESX file created: {resxPath}");
        }
    }

    public async Task GenerateExtensionsClassAsync(string nameSpace, string outputPath)
    {
        var extensionsPath = Path.Combine(outputPath, "LoczExtensions.cs");
        if (fileSystem.File.Exists(extensionsPath))
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
