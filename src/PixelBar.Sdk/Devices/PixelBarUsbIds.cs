namespace PixelBar.Sdk.Devices;

public static class PixelBarUsbIds
{
    public const ushort VendorId = 0x2D99;
    public const ushort ProductId = 0xA106;
    public const ushort UsagePage = 0xFF24;
    public const string FriendlyNameHint = "花再 Halo PixelBar";
}

public sealed record HidEndpoint(
    string DevicePath,
    string DisplayName,
    ushort UsagePage,
    ushort Usage,
    ushort InputReportLength,
    ushort OutputReportLength);
