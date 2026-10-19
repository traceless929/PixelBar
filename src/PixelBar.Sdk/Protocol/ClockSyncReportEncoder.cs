namespace PixelBar.Sdk.Protocol;

/// <summary>EC/77：B26 时间校准，使用本地年月日时分秒，保留设备的 12/24 小时设置。</summary>
public static class ClockSyncReportEncoder
{
    public static byte[] Encode(DateTimeOffset localTime)
    {
        var report = new byte[HidReportLayout.Length];
        report[0] = 0x2E;
        report[1] = 0xAA;
        report[2] = 0xEC;
        report[3] = 0x77;
        report[5] = 9;
        report[6] = (byte)(localTime.Year >> 8);
        report[7] = (byte)localTime.Year;
        report[8] = (byte)localTime.Month;
        report[9] = (byte)localTime.Day;
        report[10] = (byte)localTime.Hour;
        report[11] = (byte)localTime.Minute;
        report[12] = (byte)localTime.Second;
        report[13] = 0; // 0 保留当前制式；1/2 才会切换为 12/24 小时制。
        report[14] = localTime.Hour >= 12 ? (byte)1 : (byte)0;
        report[15] = HidReportFormatter.SumChecksum(report.AsSpan(0, 15));
        return report;
    }
}
