using System.Runtime.Versioning;
using PixelBar.Sdk;
using PixelBar.Sdk.Protocol;
using PixelBar.Cli.Parsing;

namespace PixelBar.Cli;

[SupportedOSPlatform("windows")]
public static class CliApp
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "list" => ListDevices(),
                "text" => SendText(args),
                "rgb" => SendRgb(args),
                "pattern" => SendPattern(args),
                "clock" => SendClock(args),
                "spectrum" => SendSpectrum(args),
                "screen-color" => SendScreenColor(args),
                "dry-run" => DryRun(args),
                "--help" or "-h" or "help" => PrintHelp() ? 0 : 0,
                _ => UnknownCommand(args[0]),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    static int ListDevices()
    {
        var devices = PixelBarSdk.DiscoverDevices();
        if (devices.Count == 0)
        {
            Console.WriteLine("No PixelBar screen endpoints found.");
            return 1;
        }

        for (var i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            Console.WriteLine($"[{i + 1}] {device.DisplayName}");
            Console.WriteLine($"    path: {device.DevicePath}");
            Console.WriteLine($"    usagePage: 0x{device.UsagePage:X4}, usage: 0x{device.Usage:X4}");
            Console.WriteLine($"    inputReport: {device.InputReportLength}, outputReport: {device.OutputReportLength}");
        }

        return 0;
    }

    static int SendText(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: text <message> [--scroll [ltr|rtl]] [--align left|center|right|justify]");
            return 1;
        }

        var (text, effect, alignment, scrollDirection) = TextOptionsParser.Parse(args.Skip(1).ToArray());
        PixelBarSdk.Connect().ShowText(text, effect, alignment, scrollDirection);
        var layout = effect == TextDisplayEffect.Scroll ? scrollDirection.ToString() : alignment.ToString();
        Console.WriteLine($"Sent text: {text} ({effect}, {layout})");
        return 0;
    }

    static int SendRgb(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: rgb <mode 1-6> <#RRGGBB> [speed 1-16, modes 1-2 only]");
            return 1;
        }

        if (!byte.TryParse(args[1], out var modeValue) || !Enum.IsDefined(typeof(LightMode), modeValue))
        {
            Console.Error.WriteLine("Mode must be between 1 and 6.");
            return 1;
        }

        var color = RgbColor.FromHex(args[2]);
        var mode = (LightMode)modeValue;
        var speed = args.Length > 3 && byte.TryParse(args[3], out var parsedSpeed)
            ? parsedSpeed
            : RgbReportEncoder.DefaultSpeed;

        PixelBarSdk.Connect().SetLight(mode, color, speed);
        var speedNote = RgbReportEncoder.SupportsSpeed(mode) ? $", speed={speed}" : "";
        Console.WriteLine($"Sent light: mode={mode}, color=#{color.R:X2}{color.G:X2}{color.B:X2}{speedNote} (0x77 + 0x6B)");
        return 0;
    }

    static int SendPattern(string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out var pattern))
        {
            Console.Error.WriteLine("Usage: pattern <1-11>");
            return 1;
        }

        PixelBarSdk.Connect().ShowPattern(pattern);
        Console.WriteLine($"Sent pattern: {pattern}");
        return 0;
    }

    static int SendClock(string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out var style))
        {
            Console.Error.WriteLine("Usage: clock <1-11>");
            return 1;
        }

        PixelBarSdk.Connect().ShowClock(style);
        Console.WriteLine($"Sent clock style: {style} (pattern F0 B4 C8)");
        return 0;
    }

    static int SendSpectrum(string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out var style))
        {
            Console.Error.WriteLine("Usage: spectrum <1-4>");
            return 1;
        }

        PixelBarSdk.Connect().ShowSpectrum(style);
        Console.WriteLine($"Sent spectrum style: {style} (index 0x08{style - 1:X2})");
        return 0;
    }

    static int SendScreenColor(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: screen-color <#RRGGBB> [--sync-light]");
            return 1;
        }

        var color = RgbColor.FromHex(args[1]);
        var syncLight = args.Skip(2).Any(a => a is "--sync-light" or "--sync");
        PixelBarSdk.Connect().SetScreenColor(color, syncLight);
        var syncNote = syncLight ? " + atmosphere sync (experimental)" : "";
        Console.WriteLine($"Sent screen color: #{color.R:X2}{color.G:X2}{color.B:X2} (0xEF 00 04 03){syncNote}");
        return 0;
    }

    static int DryRun(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: dry-run <text|rgb|pattern|clock|spectrum|screen-color> ...");
            return 1;
        }

        if (args[1].Equals("rgb", StringComparison.OrdinalIgnoreCase) && args.Length >= 4)
        {
            foreach (var packet in BuildRgbReports(args))
                Console.WriteLine(HidReportFormatter.ToHex(packet));
            return 0;
        }

        if (args[1].Equals("spectrum", StringComparison.OrdinalIgnoreCase) && args.Length >= 3 && int.TryParse(args[2], out var spectrumStyle))
        {
            Console.WriteLine(HidReportFormatter.ToHex(SceneReportEncoder.EncodeSpectrum(spectrumStyle)));
            return 0;
        }

        if (args[1].Equals("clock", StringComparison.OrdinalIgnoreCase) && args.Length >= 3 && int.TryParse(args[2], out var clockStyle))
        {
            Console.WriteLine(HidReportFormatter.ToHex(PatternReportEncoder.Encode(clockStyle)));
            return 0;
        }

        if (args[1].Equals("screen-color", StringComparison.OrdinalIgnoreCase) && args.Length >= 3)
        {
            var color = RgbColor.FromHex(args[2]);
            var syncLight = args.Skip(3).Any(a => a is "--sync-light" or "--sync");
            var sequence = syncLight
                ? ScreenColorReportEncoder.EncodeSetColorWithAtmosphereSync(color)
                : ScreenColorReportEncoder.EncodeSetColor(color);
            foreach (var packet in sequence)
                Console.WriteLine(HidReportFormatter.ToHex(packet));
            return 0;
        }

        if (args[1].Equals("text", StringComparison.OrdinalIgnoreCase) && args.Length >= 3)
        {
            var (text, effect, alignment, scrollDirection) = TextOptionsParser.Parse(args.Skip(2).ToArray());
            var layout = effect == TextDisplayEffect.Static
                ? TextLayoutReportEncoder.EncodeStatic(alignment)
                : TextLayoutReportEncoder.EncodeScroll(scrollDirection);
            foreach (var packet in new[] { layout, TextReportEncoder.Encode(text) })
                Console.WriteLine(HidReportFormatter.ToHex(packet));
            return 0;
        }

        var report = args[1].ToLowerInvariant() switch
        {
            "pattern" when args.Length >= 3 && int.TryParse(args[2], out var pattern) => PatternReportEncoder.Encode(pattern),
            _ => throw new ArgumentException("Unsupported dry-run payload."),
        };

        Console.WriteLine(HidReportFormatter.ToHex(report));
        return 0;
    }

    static IEnumerable<byte[]> BuildRgbReports(string[] args)
    {
        if (!byte.TryParse(args[2], out var modeValue) || !Enum.IsDefined(typeof(LightMode), modeValue))
            throw new ArgumentException("Mode must be between 1 and 6.");

        var color = RgbColor.FromHex(args[3]);
        var speed = args.Length > 4 && byte.TryParse(args[4], out var parsedSpeed)
            ? parsedSpeed
            : RgbReportEncoder.DefaultSpeed;

        yield return TempoHubLightReportEncoder.Encode(color, speed);
        yield return RgbReportEncoder.Encode((LightMode)modeValue, color, speed);
    }

    static bool PrintHelp()
    {
        Console.WriteLine("""
            PixelBar CLI — 花再 Halo PixelBar 命令行工具

            Commands:
              list                         List connected screen endpoints
              text <message> [--scroll [ltr|rtl]] [--align left|center|right|justify]
              rgb <mode> <#RRGGBB> [speed]   Set RGB lighting (speed only for modes 1-2)
              pattern <1-11>               Switch clock pattern (legacy F0 B4 C8)
              clock <1-11>                 Switch clock style
              spectrum <1-4>               Switch spectrum style
              screen-color <#RRGGBB> [--sync-light]  Set pixel screen theme color (experimental)
              dry-run <cmd> ...            Print HID packets without sending

            Global options (future): --device <path>

            Examples:
              pixelbar text "你好 PixelBar"
              pixelbar rgb 3 #00FF00
              pixelbar spectrum 1
              dotnet run --project src/PixelBar.Cli -- text "Hello" --scroll rtl

            Install / publish:
              dotnet publish src/PixelBar.Cli -c Release -r win-x64 --self-contained
            """);
        return true;
    }

    static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }
}
