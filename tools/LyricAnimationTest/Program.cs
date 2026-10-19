using System.Text;
using System.Text.Json;
using PixelBar.Sdk.Protocol;
using PixelBar_App.Services;
using PixelBar_App.Services.Lyrics;

var assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}

void Reject(Action action, string message)
{
    assertions++;
    try { action(); }
    catch (ArgumentException) { return; }
    throw new Exception(message);
}

// Wire fixtures derived from the installed TempoHub screen/lyric commands.
var enable = LyricReportEncoder.EncodeEnable();
Check(enable.AsSpan(0, 16).SequenceEqual(Convert.FromHexString("2EAAECEF000902F0B4C8000001FFFFFB")), "Enable lyric overlay separately before choosing animation");
Check(LyricReportEncoder.EncodeEnable(false).AsSpan(0, 16).SequenceEqual(Convert.FromHexString("2EAAECEF000902F0B4C8000000FFFFFA")), "Disable overlay before returning to static/scroll");
var a106First = LyricReportEncoder.EncodeAnimation(LyricAnimation.BottomToTop);
Check(a106First.AsSpan(0, 16).SequenceEqual(Convert.FromHexString("2EAAECEF000901F0B4C80000010000FC")), "A106 EC/EF fixture");
Check(LyricReportEncoder.EncodeAnimation(LyricAnimation.ExpandAndShrink).AsSpan(0,16).SequenceEqual(Convert.FromHexString("2EAAECEF000901F0B4C80000010001FD")), "Animation 2 agrees with TempoHub's observed body");
Check(LyricReportEncoder.EncodeAnimation(LyricAnimation.TopToBottomThenRightToLeft).AsSpan(0,16).SequenceEqual(Convert.FromHexString("2EAAECEF000901F0B4C8000001000400")), "Animation 5 agrees with TempoHub's observed body");
var small = LyricReportEncoder.Encode("Hi", LyricAnimation.BottomToTop);
Check(small.Count == 3 && small[2].AsSpan(0, 11).SequenceEqual(Convert.FromHexString("2EAAECE800040E02486943")), "Lyric source 0x0E and E8 fixture");
Check(small[0].SequenceEqual(enable) && small[1].SequenceEqual(a106First), "Enable, animation and text order");

foreach (var animation in Enum.GetValues<LyricAnimation>().Where(x => x != LyricAnimation.None))
{
    var packet = LyricReportEncoder.EncodeAnimation(animation);
    var payloadLength = packet[5];
    Check(packet.Length == 64, "Report length");
    Check(packet[14] == (byte)((byte)animation - 1), "EC animation index");
    Check(packet[6 + payloadLength] == (byte)packet.Skip(1).Take(5 + payloadLength).Sum(b => b), "Configuration checksum excludes report ID");
    Check(packet.Skip(7 + payloadLength).All(b => b == 0), "Configuration zero padding");
}

foreach (var text in new[] { new string('a', 54), new string('b', 55), new string('c', 56), new string('d', 255), new string('中', 60) + "😀末尾" })
{
    var expected = Encoding.UTF8.GetBytes(text);
    var packets = LyricReportEncoder.Encode(text, LyricAnimation.ExpandAndShrink).Skip(2).ToArray();
    List<byte> reassembled = [];
    Check(packets.Length == (expected.Length + 54) / 55, "Fragment count");
    foreach (var packet in packets)
    {
        var count = packet[5] - 2;
        Check(packet.Length == 64 && packet[6] == 0x0E && packet[7] == expected.Length, "Source and total length in every fragment");
        Check(packet[8 + count] == (byte)packet.Skip(1).Take(7 + count).Sum(b => b), "Fragment checksum");
        reassembled.AddRange(packet.Skip(8).Take(count));
    }
    Check(reassembled.SequenceEqual(expected), "Lossless UTF-8 reassembly across fragments");
}

Reject(() => LyricReportEncoder.EncodeAnimation(LyricAnimation.None), "None must use the static/scroll path");
Reject(() => LyricReportEncoder.EncodeAnimation((LyricAnimation)6), "Reject undefined animation");
Reject(() => LyricReportEncoder.Encode(new string('x', 256), LyricAnimation.BottomToTop), "Reject overlong payload");
Reject(() => LyricReportEncoder.Encode("  ", LyricAnimation.BottomToTop), "Reject empty lyric");
Check(LyricsDisplayFormatter.TruncateToUtf8Bytes("中😀文", 6) == "中", "Do not split emoji at byte limit");
Check(LyricsDisplayFormatter.TruncateToUtf8Bytes("中😀文", 7) == "中😀", "Keep full Unicode scalar");
Check(Encoding.UTF8.GetByteCount(LyricsDisplayFormatter.TruncateToUtf8Bytes(new string('中', 84) + "😀", 255)) == 252, "255-byte lyric limit with emoji");

var existing = JsonSerializer.Deserialize<AppSettings>("""{"LyricsEnabled":true,"LyricsTimingOffsetMs":-1900,"LyricsProvider":1,"LyricsScrollLongLines":false}""")!;
Check(existing.LyricsAnimation == LyricAnimation.BottomToTop, "Existing settings enable first animation by default");
Check(existing.LyricsEnabled && existing.LyricsTimingOffsetMs == -1900 && existing.LyricsProvider == LyricsMusicProvider.NetEaseCloudMusic && !existing.LyricsScrollLongLines, "Keep existing lyric preferences");
foreach (var animation in Enum.GetValues<LyricAnimation>())
{
    existing.LyricsAnimation = animation;
    var roundtrip = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(existing))!;
    Check(roundtrip.LyricsAnimation == animation, "Persist selected animation including None");
}

Console.WriteLine($"PASS: {assertions} lyric animation, wire packet, UTF-8 and settings checks.");
