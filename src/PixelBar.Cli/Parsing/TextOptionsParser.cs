using PixelBar.Sdk.Protocol;

namespace PixelBar.Cli.Parsing;

public static class TextOptionsParser
{
    public static (string Text, TextDisplayEffect Effect, PixelTextAlignment Alignment, TextScrollDirection ScrollDirection) Parse(string[] args)
    {
        var effect = TextDisplayEffect.Static;
        var alignment = PixelTextAlignment.Center;
        var scrollDirection = TextScrollDirection.LeftToRight;
        var textParts = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--scroll", StringComparison.OrdinalIgnoreCase))
            {
                effect = TextDisplayEffect.Scroll;
                if (i + 1 < args.Length && TryParseScrollDirection(args[i + 1], out var direction))
                {
                    scrollDirection = direction;
                    i++;
                }
                continue;
            }

            if (arg.StartsWith("--scroll=", StringComparison.OrdinalIgnoreCase))
            {
                effect = TextDisplayEffect.Scroll;
                if (!TryParseScrollDirection(arg["--scroll=".Length..], out scrollDirection))
                    throw new ArgumentException("Invalid --scroll value. Use ltr or rtl.");
                continue;
            }

            if (arg.Equals("--align", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException("Missing value for --align.");
                alignment = ParseAlignment(args[++i]);
                continue;
            }

            if (arg.StartsWith("--align=", StringComparison.OrdinalIgnoreCase))
            {
                alignment = ParseAlignment(arg["--align=".Length..]);
                continue;
            }

            textParts.Add(arg);
        }

        if (textParts.Count == 0)
            throw new ArgumentException("Text message is required.");

        return (string.Join(' ', textParts), effect, alignment, scrollDirection);
    }

    static bool TryParseScrollDirection(string value, out TextScrollDirection direction)
    {
        direction = value.ToLowerInvariant() switch
        {
            "rtl" or "right-to-left" or "right" => TextScrollDirection.RightToLeft,
            "ltr" or "left-to-right" or "left" => TextScrollDirection.LeftToRight,
            _ => default,
        };

        return value.ToLowerInvariant() is "ltr" or "left-to-right" or "left" or "rtl" or "right-to-left" or "right";
    }

    static PixelTextAlignment ParseAlignment(string value) => value.ToLowerInvariant() switch
    {
        "left" => PixelTextAlignment.Left,
        "right" => PixelTextAlignment.Right,
        "justify" or "stretch" => PixelTextAlignment.Justify,
        _ => PixelTextAlignment.Center,
    };
}
