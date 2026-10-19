using System.Text.Json;
using PixelBar.Sdk.Protocol;

namespace PixelBar_App.Services;

internal static class ClockSyncSmokeTest
{
    public static async Task RunAsync()
    {
        var checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException($"Clock sync regression: {label}");
            checks++;
        }

        var afternoon = new DateTimeOffset(2026, 10, 19, 12, 34, 56, TimeSpan.FromHours(8));
        var fixture = ClockSyncReportEncoder.Encode(afternoon);
        Check(fixture.AsSpan(0, 16).SequenceEqual(Convert.FromHexString("2EAAEC77000907EA0A130C223800018B")), "B26 packet fixture and checksum");
        foreach (var time in new[]
        {
            new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.FromHours(14)),
            new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.FromHours(-10)),
            afternoon,
        })
        {
            var packet = ClockSyncReportEncoder.Encode(time);
            Check(packet.Length == 64 && packet.Skip(16).All(x => x == 0), "64-byte zero padded report");
            Check(((packet[6] << 8) | packet[7]) == time.Year && packet[8] == time.Month && packet[9] == time.Day, "local date including leap day and year boundary");
            Check(packet[10] == time.Hour && packet[11] == time.Minute && packet[12] == time.Second, "wall time rather than UTC");
            Check(packet[13] == 0 && packet[14] == (time.Hour >= 12 ? 1 : 0), "preserve hour system and encode AM/PM");
            Check(packet[15] == (byte)packet.Skip(1).Take(14).Sum(x => x), "checksum excludes HID report ID");
        }

        var automatic = false;
        string? path = "test-screen";
        var sends = 0;
        var service = new ClockTimeSyncService(() => automatic, () => path, actualPath =>
        {
            Check(actualPath == "test-screen", "selected device is used");
            Interlocked.Increment(ref sends);
            return afternoon;
        });
        await service.TryStartupSyncAsync();
        Check(sends == 0, "disabled startup sends nothing");
        automatic = true;
        path = null;
        await service.TryStartupSyncAsync();
        Check(sends == 0, "no device defers startup sync");
        path = "test-screen";
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.TryStartupSyncAsync()));
        Check(sends == 1 && service.LastSynchronizedAt == afternoon, "concurrent startup events sync once");
        await service.TryStartupSyncAsync();
        Check(sends == 1, "page or connection events do not repeat successful startup sync");
        automatic = false;
        await service.SyncNowAsync();
        Check(sends == 2, "manual sync works with automatic sync disabled");

        var attempts = 0;
        var retry = new ClockTimeSyncService(() => true, () => "test-screen", _ =>
            ++attempts == 1 ? throw new IOException("test failure") : afternoon);
        await retry.TryStartupSyncAsync();
        Check(retry.ErrorMessage is not null && !retry.IsSyncing, "startup failure is contained and releases gate");
        await retry.TryStartupSyncAsync();
        Check(attempts == 2 && retry.LastSynchronizedAt == afternoon && retry.ErrorMessage is null, "retry after reconnect succeeds");

        var settings = JsonSerializer.Deserialize<AppSettings>("""{"LyricsEnabled":true,"LyricsTimingOffsetMs":-1900}""")!;
        Check(settings.AutoSyncClockOnStartup && settings.LyricsEnabled && settings.LyricsTimingOffsetMs == -1900, "existing preferences are preserved and startup sync defaults on");
        settings.AutoSyncClockOnStartup = false;
        Check(!JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!.AutoSyncClockOnStartup, "disabled switch persists");
        LyricsPageSmokeTest.Write($"PASS {checks} clock sync checks: packets, local time, startup once, off, manual and failure retry");
    }
}
