using System.Runtime.Versioning;
using PixelBar.Sdk.Devices;

namespace PixelBar.Sdk;

/// <summary>SDK entry point for device discovery and connection.</summary>
[SupportedOSPlatform("windows")]
public static class PixelBarSdk
{
    /// <summary>List connected PixelBar screen HID endpoints.</summary>
    public static IReadOnlyList<HidEndpoint> DiscoverDevices() =>
        PixelBarDiscovery.ListScreenEndpoints();

    /// <summary>Connect using an optional device path (first endpoint if null).</summary>
    public static PixelBarClient Connect(string? devicePath = null) =>
        new(devicePath);

    /// <summary>Connect to the first available screen endpoint.</summary>
    public static PixelBarClient ConnectPrimary()
    {
        var endpoint = PixelBarDiscovery.FindPrimary()
            ?? throw new InvalidOperationException("No PixelBar screen endpoint found.");

        return new PixelBarClient(endpoint.DevicePath);
    }
}
