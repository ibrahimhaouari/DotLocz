using System.Globalization;
using System.Text;
using CsvHelper;

namespace DotLocz;

public sealed class LoczService
{
    public static async Task ScanAndGenerateAsync(string? directory = null, string relativeOutputPath = "Locz")
    {
        var currentDirectory = directory ?? Directory.GetCurrentDirectory();

        // Find Projects (.csproj) under the directory
        var projectPaths = Directory.GetFiles(currentDirectory, "*.csproj", SearchOption.AllDirectories);

        foreach (var projectPath in projectPaths)
        {
            // Find CSV files
            var csvPaths = Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*.loc.csv", SearchOption.AllDirectories);

            if (csvPaths.Length > 0)
            {
                Console.WriteLine($"Found {csvPaths.Length} CSV files in {Path.GetDirectoryName(projectPath)}");
                var outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, relativeOutputPath);
                var nameSpace = $"{Path.GetFileNameWithoutExtension(projectPath)}.{relativeOutputPath.Replace("/", ".")}";

                Directory.CreateDirectory(outputPath);


                foreach (var csvPath in csvPaths)
                {
                    var resourceName = Path.GetFileNameWithoutExtension(csvPath).Replace(".loc", "");

                    // Check if csv file changed since last generation
                    var shouldGenerate = false;
                    var enumPath = Path.Combine(outputPath, $"{resourceName}.cs");
                    // Check if enum file exists
                    if (!File.Exists(enumPath))
                    {
                        shouldGenerate = true;
                    }
                    else
                    {
                        var csvLastWriteTime = File.GetLastWriteTime(csvPath);
                        var enumLastWriteTime = File.GetLastWriteTime(enumPath);
                        if (csvLastWriteTime > enumLastWriteTime)
                        {
                            shouldGenerate = true;
                        }
                    }

                    if (shouldGenerate)
                    {
                        await GenerateEnumAsync(csvPath, nameSpace, resourceName, outputPath);
                        await GenerateResxAsync(csvPath, resourceName, outputPath);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping {resourceName} as it is up to date");
                    }
                }

                await GenerateExtensionsAsync(nameSpace, outputPath);
            }
            else
            {
                Console.WriteLine($"No CSV files found in {Path.GetDirectoryName(projectPath)}");
                continue;
            }
        }
    }

    public static async Task GenerateEnumAsync(string csvPath, string nameSpace, string resourceName, string outputPath)
    {
        var enumContent = new StringBuilder();
        enumContent.AppendLine($"namespace {nameSpace};");
        enumContent.AppendLine($"public enum {resourceName} {{");

        // Read CSV
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();// Skip header
        while (csv.Read())
        {
            var key = csv.GetField<string>(0);
            Console.WriteLine(key);
            enumContent.AppendLine($"    {key},");
        }


        enumContent.AppendLine("}");

        // Write to file
        var enumPath = Path.Combine(outputPath, $"{resourceName}.cs");
        await File.WriteAllTextAsync(enumPath, enumContent.ToString());
    }

    public static async Task GenerateResxAsync(string csvPath, string resourceName, string outputPath)
    {
        // Read CSV
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Languages
        csv.Read();
        var langCount = csv.ColumnCount - 1;
        var Languages = new string[langCount];
        var resxContents = new StringBuilder[langCount];
        for (var i = 0; i < langCount; i++)
        {
            var lang = csv.GetField(i + 1)!;
            Languages[i] = lang;
            resxContents[i] = new StringBuilder();
            resxContents[i].AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            resxContents[i].AppendLine("<root>");
        }

        // Entries
        while (csv.Read())
        {
            var key = csv.GetField<string>(0);
            for (var i = 0; i < langCount; i++)
            {
                var value = csv.GetField(i + 1)!;
                resxContents[i].AppendLine($"  <data name=\"{key}\" xml:space=\"preserve\">");
                resxContents[i].AppendLine($"    <value>{value}</value>");
                resxContents[i].AppendLine("  </data>");
            }
        }

        // Close
        for (var i = 0; i < langCount; i++)
        {
            resxContents[i].AppendLine("</root>");
        }


        // Write to files
        for (var i = 0; i < langCount; i++)
        {
            var resxPath = Path.Combine(outputPath, $"{resourceName}.{Languages[i]}.resx");
            await File.WriteAllTextAsync(resxPath, resxContents[i].ToString());
        }
    }

    public static async Task GenerateExtensionsAsync(string nameSpace, string outputPath)
    {
        var extensionsPath = Path.Combine(outputPath, "LoczExtensions.cs");
        if (File.Exists(extensionsPath))
        {
            return;
        }

        var content = """
        using Microsoft.Extensions.Localization;

        namespace DotLocz.Demo.Locz;

        public static class LoczExtensions
        {
            public static string Get(this IStringLocalizer localizer, Enum key) =>
                localizer[key.ToString()];
        }
        """;

        // Write to file
        await File.WriteAllTextAsync(extensionsPath, content);
    }

}