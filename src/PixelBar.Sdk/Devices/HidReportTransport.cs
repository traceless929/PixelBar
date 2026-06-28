using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using PixelBar.Sdk.Protocol;

namespace PixelBar.Sdk.Devices;

[SupportedOSPlatform("windows")]
internal static class HidReportTransport
{
    public static void Send(ReadOnlySpan<byte> report, string? devicePath = null)
    {
        if (report.Length != HidReportLayout.Length)
            throw new ArgumentException($"Report must be exactly {HidReportLayout.Length} bytes.", nameof(report));

        var path = devicePath ?? PixelBarDiscovery.FindPrimary()?.DevicePath
            ?? throw new InvalidOperationException(
                $"No PixelBar screen endpoint found ({PixelBarUsbIds.FriendlyNameHint}, Col02, {HidReportLayout.Length}-byte report).");

        using var handle = WinHidNative.OpenDevice(path);
        var buffer = report.ToArray();
        if (!WinHidNative.WriteReport(handle, buffer, out var written) || written != buffer.Length)
            throw new IOException($"WriteFile failed for {path}.");
    }
}

[SupportedOSPlatform("windows")]
internal static class WinHidNative
{
    private const uint GenericRead = 0x8000_0000;
    private const uint GenericWrite = 0x4000_0000;
    private const uint FileShareRead = 0x0000_0001;
    private const uint FileShareWrite = 0x0000_0002;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x0000_0080;

    public static SafeFileHandle OpenDevice(string path)
    {
        var handle = CreateFileW(
            path,
            GenericRead | GenericWrite,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal,
            IntPtr.Zero);

        if (handle.IsInvalid)
            throw new IOException($"Unable to open HID device: {path}", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));

        return handle;
    }

    public static bool WriteReport(SafeFileHandle handle, byte[] buffer, out int bytesWritten)
    {
        return WriteFile(handle, buffer, buffer.Length, out bytesWritten, IntPtr.Zero);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFileW(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle handle,
        byte[] buffer,
        int numberOfBytesToWrite,
        out int numberOfBytesWritten,
        IntPtr overlapped);
}
