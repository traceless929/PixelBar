using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using PixelBar.Sdk.Protocol;

namespace PixelBar.Sdk.Devices;

[SupportedOSPlatform("windows")]
public static class PixelBarDiscovery
{
    public static IReadOnlyList<HidEndpoint> ListScreenEndpoints()
    {
        var matches = new List<HidEndpoint>();
        var deviceInfoSet = WinSetupNative.CreateDeviceInfoSet();
        if (deviceInfoSet == WinSetupNative.InvalidHandle)
            throw new InvalidOperationException("SetupDiGetClassDevsW failed.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));

        try
        {
            for (var index = 0; ; index++)
            {
                if (!WinSetupNative.TryEnumDeviceInterface(deviceInfoSet, index, out var interfaceData))
                    break;

                if (!WinSetupNative.TryGetDeviceInterfaceDetail(deviceInfoSet, interfaceData, out var path, out var deviceInfo))
                    continue;

                if (!IsPixelBarScreenPath(path))
                    continue;

                var friendlyName = WinSetupNative.TryGetFriendlyName(deviceInfoSet, deviceInfo);
                if (WinSetupNative.TryProbeEndpoint(path, friendlyName) is { } endpoint)
                    matches.Add(endpoint);
            }
        }
        finally
        {
            WinSetupNative.DestroyDeviceInfoSet(deviceInfoSet);
        }

        return matches;
    }

    public static HidEndpoint? FindPrimary() => ListScreenEndpoints().FirstOrDefault();

    private static bool IsPixelBarScreenPath(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.Contains($"vid_{PixelBarUsbIds.VendorId:x4}")
            && lower.Contains($"pid_{PixelBarUsbIds.ProductId:x4}")
            && lower.Contains("col02");
    }
}

[SupportedOSPlatform("windows")]
internal static class WinSetupNative
{
    public static readonly IntPtr InvalidHandle = new(-1);

    private const uint DigcfPresent = 0x0000_0002;
    private const uint DigcfDeviceInterface = 0x0000_0010;
    private const uint SpdrpFriendlyName = 0x0000_000C;
    private static readonly Guid HidInterfaceClass = new("4D1E55B2-F16F-11CF-88CB-001111000030");
    private static int DetailCbSize => IntPtr.Size == 8 ? 8 : 6;

    public static IntPtr CreateDeviceInfoSet()
    {
        var classGuid = HidInterfaceClass;
        return SetupDiGetClassDevsW(ref classGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
    }

    public static void DestroyDeviceInfoSet(IntPtr deviceInfoSet)
    {
        if (deviceInfoSet != InvalidHandle && deviceInfoSet != IntPtr.Zero)
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
    }

    public static bool TryEnumDeviceInterface(IntPtr deviceInfoSet, int index, out SpDeviceInterfaceData interfaceData)
    {
        interfaceData = new SpDeviceInterfaceData { Size = Marshal.SizeOf<SpDeviceInterfaceData>() };
        var classGuid = HidInterfaceClass;
        return SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref classGuid, index, ref interfaceData);
    }

    public static bool TryGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        SpDeviceInterfaceData interfaceData,
        out string path,
        out SpDevinfoData deviceInfo)
    {
        path = string.Empty;
        deviceInfo = new SpDevinfoData { Size = Marshal.SizeOf<SpDevinfoData>() };

        SetupDiGetDeviceInterfaceDetailW(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out var requiredSize, ref deviceInfo);
        if (requiredSize == 0)
            return false;

        var buffer = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            Marshal.WriteInt32(buffer, DetailCbSize);
            if (!SetupDiGetDeviceInterfaceDetailW(deviceInfoSet, ref interfaceData, buffer, requiredSize, out _, ref deviceInfo))
                return false;

            path = Marshal.PtrToStringUni(buffer + sizeof(int)) ?? string.Empty;
            return path.Length > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static string TryGetFriendlyName(IntPtr deviceInfoSet, SpDevinfoData deviceInfo)
    {
        var buffer = Marshal.AllocHGlobal(512);
        try
        {
            if (!SetupDiGetDeviceRegistryPropertyW(
                    deviceInfoSet,
                    ref deviceInfo,
                    SpdrpFriendlyName,
                    out _,
                    buffer,
                    512,
                    out _))
                return string.Empty;

            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static HidEndpoint? TryProbeEndpoint(string path, string friendlyName)
    {
        SafeFileHandle handle;
        try
        {
            handle = WinHidNative.OpenDevice(path);
        }
        catch (IOException)
        {
            return null;
        }

        using (handle)
        {
            var product = ReadHidString(handle, HidD_GetProductString);
            var displayName = ComposeDisplayName(friendlyName, product);

            if (!HidD_GetPreparsedData(handle, out var preparsedData))
                return null;

            try
            {
                if (HidP_GetCaps(preparsedData, out var caps) < 0)
                    return null;

                if (caps.InputReportByteLength != HidReportLayout.Length)
                    return null;

                return new HidEndpoint(
                    path,
                    displayName,
                    caps.UsagePage,
                    caps.Usage,
                    caps.InputReportByteLength,
                    caps.OutputReportByteLength);
            }
            finally
            {
                HidD_FreePreparsedData(preparsedData);
            }
        }
    }

    private static string ComposeDisplayName(string friendlyName, string product)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
            return product;

        if (string.IsNullOrWhiteSpace(product) || friendlyName.Contains(product, StringComparison.Ordinal))
            return friendlyName;

        return $"{friendlyName} ({product})";
    }

    private static string ReadHidString(SafeFileHandle handle, HidStringReader reader)
    {
        var buffer = Marshal.AllocHGlobal(512);
        try
        {
            return reader(handle, buffer, 512) ? Marshal.PtrToStringUni(buffer) ?? string.Empty : string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private delegate bool HidStringReader(SafeFileHandle handle, IntPtr buffer, int size);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiGetClassDevsW")]
    private static extern IntPtr SetupDiGetClassDevsW(
        ref Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        int memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetailW")]
    private static extern bool SetupDiGetDeviceInterfaceDetailW(
        IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        ref SpDevinfoData deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiGetDeviceRegistryPropertyW")]
    private static extern bool SetupDiGetDeviceRegistryPropertyW(
        IntPtr deviceInfoSet,
        ref SpDevinfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        IntPtr propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetProductString(SafeFileHandle handle, IntPtr buffer, int bufferLength);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(IntPtr preparsedData, out HidpCaps caps);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpDeviceInterfaceData
    {
        public int Size;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpDevinfoData
    {
        public int Size;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HidpCaps
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        public ushort Reserved0;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public ushort Reserved4;
        public ushort Reserved5;
        public ushort Reserved6;
        public ushort Reserved7;
        public ushort Reserved8;
        public ushort Reserved9;
        public ushort Reserved10;
        public ushort Reserved11;
        public ushort Reserved12;
        public ushort Reserved13;
        public ushort Reserved14;
        public ushort Reserved15;
        public ushort Reserved16;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }
}
