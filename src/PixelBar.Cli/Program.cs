using PixelBar.Cli;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("PixelBar CLI requires Windows.");
    return 1;
}

return CliApp.Run(args);
