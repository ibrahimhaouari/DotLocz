using System.IO.Abstractions;
using DotLocz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<IFileSystem, FileSystem>();
builder.Services.AddScoped<ILoczService, LoczService>();

using IHost host = builder.Build();

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
    var loczService = host.Services.GetRequiredService<ILoczService>();
    // Call the service to scan for CSV files and generate resources
    await loczService.ScanAndGenerateAsync(directory, relativeOutputPath);

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
