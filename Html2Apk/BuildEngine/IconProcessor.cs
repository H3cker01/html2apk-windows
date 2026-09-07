using System.IO;

namespace Html2Apk.BuildEngine;

/// <summary>
/// Resizes the user's icon PNG into Android mipmap densities.
/// Uses WPF/GDI+ via System.Drawing (bundled in .NET 8 Windows).
/// </summary>
public static class IconProcessor
{
    private static readonly Dictionary<string, int> MipmapSizes = new()
    {
        ["mipmap-mdpi"]    = 48,
        ["mipmap-hdpi"]    = 72,
        ["mipmap-xhdpi"]   = 96,
        ["mipmap-xxhdpi"]  = 144,
        ["mipmap-xxxhdpi"] = 192,
    };

    public static void Process(string iconPath, string resDir)
    {
        foreach (var (folder, size) in MipmapSizes)
        {
            var dir = Path.Combine(resDir, folder);
            Directory.CreateDirectory(dir);
            var dest = Path.Combine(dir, "ic_launcher.png");

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                ResizeImage(iconPath, dest, size);
            }
            else
            {
                GenerateDefaultIcon(dest, size);
            }
        }
    }

    private static void ResizeImage(string src, string dest, int size)
    {
        using var bmp = new System.Drawing.Bitmap(src);
        using var resized = new System.Drawing.Bitmap(bmp, new System.Drawing.Size(size, size));
        resized.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void GenerateDefaultIcon(string dest, int size)
    {
        using var bmp = new System.Drawing.Bitmap(size, size);
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(124, 106, 247));
        int q = size / 4;
        g.FillEllipse(System.Drawing.Brushes.White, q, q, size / 2, size / 2);
        bmp.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
    }
}
