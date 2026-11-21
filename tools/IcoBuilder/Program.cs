// Builds a multi-size .ico (PNG-compressed entries, Vista+) from a square PNG,
// masking the corners to a modern rounded squircle with antialiased edges.
// usage: dotnet run -- <input.png> <output.ico> [radiusFactor=0.22]
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("usage: IcoBuilder <input.png> <output.ico> [radiusFactor=0.22]");
    return 1;
}

var input = args[0];
var output = args[1];
var radiusFactor = args.Length == 3 && float.TryParse(args[2], out var f) && f is > 0f and < 0.5f ? f : 0.22f;

var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };

using var src = Image.FromFile(input);
var pngs = new List<byte[]>();
foreach (var s in sizes)
{
    // scale to size
    using var scaled = new Bitmap(s, s);
    using (var g = Graphics.FromImage(scaled))
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(src, 0, 0, s, s);
    }

    // round the corners (transparent outside the mask)
    using var rounded = new Bitmap(s, s);
    using (var og = Graphics.FromImage(rounded))
    {
        og.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPath(s, (float)(s * radiusFactor));
        using var texture = new TextureBrush(scaled);
        og.FillPath(texture, path);
    }

    using var ms = new MemoryStream();
    rounded.Save(ms, ImageFormat.Png);
    pngs.Add(ms.ToArray());
}

using (var fs = File.Create(output))
using (var w = new BinaryWriter(fs))
{
    // ICONDIR
    w.Write((ushort)0);            // reserved
    w.Write((ushort)1);            // type: icon
    w.Write((ushort)sizes.Length); // entry count

    uint offset = 6 + (uint)(16 * sizes.Length);
    for (var i = 0; i < sizes.Length; i++)
    {
        var s = sizes[i];
        w.Write((byte)(s == 256 ? 0 : s));  // width (0 = 256)
        w.Write((byte)(s == 256 ? 0 : s));  // height
        w.Write((byte)0);                    // palette count
        w.Write((byte)0);                    // reserved
        w.Write((ushort)1);                  // color planes
        w.Write((ushort)32);                 // bits per pixel
        w.Write((uint)pngs[i].Length);       // entry size
        w.Write(offset);                     // entry offset
        offset += (uint)pngs[i].Length;
    }
    foreach (var p in pngs)
    {
        w.Write(p);
    }
}

Console.WriteLine($"wrote {output} ({sizes.Length} entries: {string.Join(", ", sizes)}; corner radius {radiusFactor:P0})");
return 0;

static GraphicsPath RoundedPath(int s, float radius)
{
    var d = radius * 2;
    var path = new GraphicsPath();
    path.AddArc(0, 0, d, d, 180, 90);
    path.AddArc(s - d, 0, d, d, 270, 90);
    path.AddArc(s - d, s - d, d, d, 0, 90);
    path.AddArc(0, s - d, d, d, 90, 90);
    path.CloseFigure();
    return path;
}
