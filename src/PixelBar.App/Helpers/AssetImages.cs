namespace PixelBar_App.Helpers;

using Microsoft.UI.Xaml.Media.Imaging;

/// <summary>加载随 exe 部署的 Assets 文件（unpackaged WinUI 不能用 ms-appx  alone）。</summary>
public static class AssetImages
{
    public static string GetPath(string relativeAssetPath)
    {
        var normalized = relativeAssetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, normalized));
    }

    public static Uri GetUri(string relativeAssetPath)
    {
        var path = GetPath(relativeAssetPath);
        return File.Exists(path)
            ? new Uri(path)
            : new Uri($"ms-appx:///{relativeAssetPath}");
    }

    public static BitmapImage LoadBitmap(string relativeAssetPath) =>
        new(GetUri(relativeAssetPath));
}
