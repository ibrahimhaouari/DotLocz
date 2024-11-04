using DotLocz;

// get args
var directory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var relativeOutputPath = args.Length > 1 ? args[1] : "Locz";

await LoczService.ScanAndGenerateAsync(directory, relativeOutputPath);
