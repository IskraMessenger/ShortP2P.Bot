using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>Draws a weather scene and pads it so the PNG is about 100–150 KB (several blob chunks).</summary>
internal static class WeatherPicture
{
    public const int MinBytes = 100 * 1024;
    public const int MaxBytes = 150 * 1024;

    private const int Width = 800;
    private const int SceneHeight = 480;

    public static byte[] Render(string conditionKey)
    {
        var key = string.IsNullOrWhiteSpace(conditionKey) ? "dry" : conditionKey.Trim();
        var plain = Compose(key, 0);
        var probeRows = 40;
        var probe = Compose(key, probeRows);
        var perRow = Math.Max(256, (probe.Length - plain.Length) / probeRows);
        var target = (MinBytes + MaxBytes) / 2;
        var rows = (int)Math.Ceiling(Math.Max(0, target - plain.Length) / (double)perRow);
        rows = Math.Clamp(rows, 1, 400);

        var png = Compose(key, rows);
        for (var attempt = 0; attempt < 6 && png.Length is < MinBytes or > MaxBytes; attempt++)
        {
            if (png.Length < MinBytes)
                rows += Math.Max(1, (MinBytes - png.Length) / perRow);
            else
                rows -= Math.Max(1, (png.Length - MaxBytes) / perRow);
            rows = Math.Clamp(rows, 1, 400);
            png = Compose(key, rows);
        }

        if (png.Length is < MinBytes or > MaxBytes)
            throw new InvalidOperationException($"Weather PNG is {png.Length} bytes; expected {MinBytes}..{MaxBytes}.");

        return png;
    }

    private static byte[] Compose(string key, int noiseRows)
    {
        using var bitmap = new Bitmap(Width, SceneHeight + noiseRows, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Draw(graphics, key, Width, SceneHeight);
        }

        if (noiseRows > 0)
            FillNoise(bitmap, SceneHeight, noiseRows, new Random(Seed(key)));

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static int Seed(string conditionKey)
    {
        var hash = 17;
        foreach (var ch in conditionKey)
            hash = unchecked(hash * 31 + ch);
        return hash;
    }

    private static void FillNoise(Bitmap bitmap, int top, int rows, Random random)
    {
        var rect = new Rectangle(0, top, bitmap.Width, rows);
        var data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = data.Stride;
            if (stride <= 0)
                throw new InvalidOperationException("Unexpected bitmap stride.");

            var bytes = new byte[stride * rows];
            random.NextBytes(bytes);
            for (var i = 3; i < bytes.Length; i += 4)
                bytes[i] = 255;
            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static void Draw(Graphics g, string key, int w, int h)
    {
        switch (key)
        {
            case "sun":
                DrawSun(g, w, h);
                break;
            case "cloudy":
                DrawCloudy(g, w, h);
                break;
            case "rain":
                DrawRain(g, w, h, heavy: true);
                break;
            case "drizzle":
                DrawRain(g, w, h, heavy: false);
                break;
            case "snow":
                DrawSnow(g, w, h);
                break;
            case "sleet":
                DrawSleet(g, w, h);
                break;
            case "fog":
                DrawFog(g, w, h);
                break;
            case "thunderstorm":
                DrawStorm(g, w, h);
                break;
            default:
                DrawDry(g, w, h);
                break;
        }
    }

    private static void DrawSun(Graphics g, int w, int h)
    {
        FillSky(g, w, h, Color.FromArgb(56, 156, 224), Color.FromArgb(186, 224, 248));
        using var ray = new Pen(Color.FromArgb(255, 214, 90), 8);
        var cx = 230;
        var cy = 170;
        for (var i = 0; i < 12; i++)
        {
            var angle = i * Math.PI / 6;
            g.DrawLine(
                ray,
                cx + (float)Math.Cos(angle) * 90,
                cy + (float)Math.Sin(angle) * 90,
                cx + (float)Math.Cos(angle) * 150,
                cy + (float)Math.Sin(angle) * 150);
        }

        using var sun = new SolidBrush(Color.FromArgb(255, 204, 46));
        g.FillEllipse(sun, cx - 70, cy - 70, 140, 140);
        DrawGround(g, w, h, Color.FromArgb(92, 168, 86));
    }

    private static void DrawCloudy(Graphics g, int w, int h)
    {
        FillSky(g, w, h, Color.FromArgb(96, 124, 150), Color.FromArgb(186, 198, 210));
        DrawCloud(g, 80, 90, 280, 120, Color.FromArgb(236, 240, 244));
        DrawCloud(g, 300, 60, 340, 140, Color.White);
        DrawCloud(g, 480, 130, 260, 110, Color.FromArgb(210, 218, 226));
        DrawGround(g, w, h, Color.FromArgb(120, 140, 110));
    }

    private static void DrawRain(Graphics g, int w, int h, bool heavy)
    {
        FillSky(g, w, h, Color.FromArgb(48, 64, 84), Color.FromArgb(110, 126, 146));
        DrawCloud(g, 40, 40, 360, 130, Color.FromArgb(70, 80, 96));
        DrawCloud(g, 280, 20, 400, 150, Color.FromArgb(54, 62, 78));
        DrawGround(g, w, h, Color.FromArgb(62, 86, 64));
        using var rain = new Pen(heavy ? Color.FromArgb(120, 176, 230) : Color.FromArgb(170, 200, 230), heavy ? 3 : 2);
        var step = heavy ? 28 : 46;
        for (var x = 40; x < w; x += step)
        {
            for (var y = 180; y < h - 70; y += heavy ? 36 : 58)
                g.DrawLine(rain, x, y, x - 10, y + (heavy ? 26 : 16));
        }
    }

    private static void DrawSnow(Graphics g, int w, int h)
    {
        FillSky(g, w, h, Color.FromArgb(168, 186, 204), Color.FromArgb(230, 236, 242));
        DrawCloud(g, 60, 30, 300, 100, Color.FromArgb(246, 248, 250));
        DrawCloud(g, 340, 50, 320, 110, Color.White);
        DrawGround(g, w, h, Color.FromArgb(236, 240, 244));
        using var flake = new SolidBrush(Color.White);
        for (var i = 0; i < 40; i++)
        {
            var x = 30 + (i * 47) % (w - 60);
            var y = 160 + (i * 29) % (h - 230);
            var size = 8 + (i % 5) * 3;
            g.FillEllipse(flake, x, y, size, size);
        }
    }

    private static void DrawSleet(Graphics g, int w, int h)
    {
        DrawRain(g, w, h, heavy: true);
        using var flake = new SolidBrush(Color.White);
        for (var i = 0; i < 18; i++)
        {
            var x = 50 + (i * 73) % (w - 80);
            var y = 190 + (i * 31) % (h - 260);
            g.FillEllipse(flake, x, y, 14, 14);
        }
    }

    private static void DrawFog(Graphics g, int w, int h)
    {
        FillSky(g, w, h, Color.FromArgb(150, 156, 162), Color.FromArgb(210, 214, 216));
        using var sun = new SolidBrush(Color.FromArgb(80, 230, 230, 210));
        g.FillEllipse(sun, 500, 70, 90, 90);
        for (var i = 0; i < 6; i++)
        {
            using var band = new SolidBrush(Color.FromArgb(90 + i * 8, 200, 204, 208));
            g.FillRectangle(band, 0, 80 + i * 48, w, 36);
        }

        DrawGround(g, w, h, Color.FromArgb(150, 154, 148));
    }

    private static void DrawStorm(Graphics g, int w, int h)
    {
        FillSky(g, w, h, Color.FromArgb(22, 24, 48), Color.FromArgb(58, 52, 82));
        DrawCloud(g, 20, 20, 420, 140, Color.FromArgb(32, 34, 52));
        DrawCloud(g, 300, 10, 420, 150, Color.FromArgb(24, 26, 40));
        DrawGround(g, w, h, Color.FromArgb(40, 58, 42));
        using var bolt = new Pen(Color.FromArgb(255, 230, 70), 8);
        g.DrawLines(bolt, new[]
        {
            new Point(430, 120),
            new Point(390, 220),
            new Point(450, 230),
            new Point(360, 380)
        });
        using var rain = new Pen(Color.FromArgb(90, 140, 200), 3);
        for (var x = 60; x < w; x += 34)
            g.DrawLine(rain, x, 200, x - 12, 250);
    }

    private static void DrawDry(Graphics g, int w, int h)
    {
        FillSky(g, w, h, Color.FromArgb(176, 206, 214), Color.FromArgb(232, 220, 186));
        using var sun = new SolidBrush(Color.FromArgb(230, 196, 120));
        g.FillEllipse(sun, 620, 60, 70, 70);
        DrawGround(g, w, h, Color.FromArgb(176, 154, 96));
    }

    private static void FillSky(Graphics g, int w, int h, Color top, Color bottom)
    {
        using var brush = new LinearGradientBrush(new Rectangle(0, 0, w, h), top, bottom, LinearGradientMode.Vertical);
        g.FillRectangle(brush, 0, 0, w, h);
    }

    private static void DrawGround(Graphics g, int w, int h, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, 0, h - 70, w, 70);
    }

    private static void DrawCloud(Graphics g, int x, int y, int w, int h, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x, y + h / 3, w, h);
        g.FillEllipse(brush, x + w / 5, y, (int)(w * 0.7), (int)(h * 0.9));
        g.FillEllipse(brush, x + w / 2, y + h / 4, w / 2, h);
    }
}
