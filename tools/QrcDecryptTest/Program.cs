using PixelBar_App.Services.Lyrics;
using QrcDecryptTest;

var targetPath = args.Length > 0
    ? args[0]
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Tencent", "QQMusic", "QQMusicLyricNew");

if (File.Exists(targetPath) && targetPath.EndsWith(".qrc", StringComparison.OrdinalIgnoreCase))
{
    await TestFileAsync(targetPath);
    return;
}

if (Directory.Exists(targetPath))
{
    var files = Directory.EnumerateFiles(targetPath, "*_qm.qrc", SearchOption.TopDirectoryOnly)
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .Take(args.Contains("--all") ? int.MaxValue : 3)
        .ToList();

    if (files.Count == 0)
    {
        Console.WriteLine($"目录内未找到 *_qm.qrc: {targetPath}");
        return;
    }

    foreach (var file in files)
        await TestFileAsync(file);

    return;
}

Console.WriteLine("用法: dotnet run -- [qrc 文件或 QQMusicLyricNew 目录] [--all]");
Console.WriteLine("默认目录: %AppData%\\Tencent\\QQMusic\\QQMusicLyricNew");

var registryPath = QqMusicPath.TryGetLyricDirectory();
if (registryPath is not null)
{
    Console.WriteLine($"尝试注册表路径: {registryPath}");
    foreach (var file in Directory.EnumerateFiles(registryPath, "*_qm.qrc").Take(3))
        await TestFileAsync(file);
}

static async Task TestFileAsync(string path)
{
    Console.WriteLine(new string('=', 72));
    Console.WriteLine($"文件: {Path.GetFileName(path)}");
    Console.WriteLine($"大小: {new FileInfo(path).Length} bytes");

    var bytes = await File.ReadAllBytesAsync(path);
    Console.WriteLine($"文件头: {Convert.ToHexString(bytes.AsSpan(0, Math.Min(16, bytes.Length)))}");

    try
    {
        var result = QmQrcDecoder.TryDecrypt(bytes);
        if (result is null)
        {
            Console.WriteLine("[QmQrcDecoder] 失败");
            return;
        }

        Console.WriteLine($"[QmQrcDecoder] 成功! 长度={result.Length}");
        var flat = result.Replace('\r', ' ').Replace('\n', ' ');
        Console.WriteLine(flat.Length <= 240 ? flat : flat[..240] + "...");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[QmQrcDecoder] 异常: {ex.Message}");
    }
}
