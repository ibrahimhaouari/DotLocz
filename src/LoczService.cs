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

                foreach (var csvPath in csvPaths)
                {
                    var outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, relativeOutputPath);
                    await GenerateEnumAsync(projectPath, csvPath, relativeOutputPath);
                    await GenerateResxAsync(projectPath, csvPath, relativeOutputPath);
                }
            }
            else
            {
                Console.WriteLine($"No CSV files found in {Path.GetDirectoryName(projectPath)}");
                continue;
            }
        }
    }

    public static async Task GenerateEnumAsync(string projectPath, string csvPath, string relativeOutputPath)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var projectDirectory = Path.GetDirectoryName(projectPath);
        var enumName = Path.GetFileNameWithoutExtension(csvPath).Replace(".loc", "");
        var enumContent = new StringBuilder();

        var namespaceName = $"{projectName}.{relativeOutputPath.Replace("/", ".")}";

        enumContent.AppendLine($"namespace {namespaceName};");
        enumContent.AppendLine($"public enum {enumName} {{");

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
        var outputDirectory = Path.Combine(projectDirectory, relativeOutputPath);
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"{enumName}.cs");
        await File.WriteAllTextAsync(outputPath, enumContent.ToString());
    }

    public static async Task GenerateResxAsync(string projectPath, string csvPath, string relativeOutputPath)
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
        var enumName = Path.GetFileNameWithoutExtension(csvPath).Replace(".loc", "");
        var projectDirectory = Path.GetDirectoryName(projectPath);
        var outputDirectory = Path.Combine(projectDirectory, relativeOutputPath);
        Directory.CreateDirectory(outputDirectory);
        for (var i = 0; i < langCount; i++)
        {
            var outputPath = Path.Combine(outputDirectory, $"{enumName}.{Languages[i]}.resx");
            await File.WriteAllTextAsync(outputPath, resxContents[i].ToString());
        }
    }
}