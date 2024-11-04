using DotLocz;

// Parse the arguments to get the working directory and output path
var directory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var relativeOutputPath = args.Length > 1 ? args[1] : "Locz";

// Display starting message with the directory and output path
Console.WriteLine($"""
═════════════════════════════════════════════════════════════════════════════════════════
DotLocz - .NET Localization Tool
Starting Localization Tool
Scanning Directory: {directory}
Output Directory for Generated Files: {relativeOutputPath}
═════════════════════════════════════════════════════════════════════════════════════════
""");


try
{
    // Call the service to scan for CSV files and generate resources
    await LoczService.ScanAndGenerateAsync(directory, relativeOutputPath);

    Console.WriteLine($"[{DateTime.Now}] Localization generation completed successfully.");
}
catch (Exception ex)
{
    // Log any exceptions that occur during the execution
    Console.WriteLine($"[{DateTime.Now}] Error: {ex.Message}");
    Console.WriteLine($"[{DateTime.Now}] StackTrace: {ex.StackTrace}");
}

// Display completion message
Console.WriteLine($"[{DateTime.Now}] Tool execution finished.");
