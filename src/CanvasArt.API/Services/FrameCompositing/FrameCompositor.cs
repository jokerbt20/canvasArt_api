using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CanvasArt.API.Services.FrameCompositing;

/// <summary>
/// Wraps a picture frame around a painting image, built from ONE corner photo. The painting is
/// never resized, rotated or altered — the frame is constructed OUTSIDE it, so the returned
/// image is larger by 2 * moulding-width on each axis. Ported from the SkiaSharp proof-of-concept
/// (FramingProject/FramingApi/FrameBuilder.cs) onto ImageSharp so it fits this codebase's existing
/// image-processing stack.
/// </summary>
public static class FrameCompositor
{
    /// <param name="painting">The picture — used exactly as-is.</param>
    /// <param name="corner">A single frame corner as a transparent-background PNG (top-left).</param>
    /// <param name="frameWidthPx">Moulding width in px. If null, uses <paramref name="frac"/>.</param>
    /// <param name="frac">Moulding width as a fraction of the shorter side (default 0.15).</param>
    /// <param name="shadow">Add a subtle inner shadow so the picture sits in the rebate.</param>
    public static Image<Rgba32> BuildFrame(
        Image<Rgba32> painting,
        Image<Rgba32> corner,
        int? frameWidthPx = null,
        double frac = 0.15,
        bool shadow = true)
    {
        int w = painting.Width, h = painting.Height;

        // 1. measure the moulding from the corner's alpha channel
        var (tTop, tLeft) = DetectBands(corner, alphaThresh: 40);

        // 2. target uniform moulding width
        int t = frameWidthPx ?? (int)Math.Round(frac * Math.Min(w, h));
        t = Math.Max(8, t);

        // 3. normalise the corner so BOTH arms equal T (anisotropic stretch).
        //    top arm is vertical (tTop) -> drives height; left arm is horizontal.
        int newW = Math.Max(t + 2, (int)Math.Round(corner.Width * (double)t / tLeft));
        int newH = Math.Max(t + 2, (int)Math.Round(corner.Height * (double)t / tTop));
        using var norm = Resize(corner, newW, newH);

        // 4. authentic corner block (fully-opaque L-union corner) = T x T, mirrored
        using var tl = Crop(norm, 0, 0, t, t);
        using var tr = FlipH(tl);
        using var bl = FlipV(tl);
        using var br = FlipV(FlipH(tl));

        // 5. seamless straight-edge tiles, taken just past the corner
        int period = DetectPeriod(norm, t);
        if (period <= 0) period = 56;

        int availH = newW - t; // horizontal straight run length
        int segLen = Math.Min(period * 2, availH);
        segLen = Math.Max(period, (segLen / period) * period);
        segLen = Math.Min(segLen, availH);
        using var topStrip = Crop(norm, t, 0, segLen, t);

        int availV = newH - t; // vertical straight run length
        int segLenV = Math.Min(segLen, availV);
        using var leftSeg = Crop(norm, 0, t, t, segLenV);

        using var topEdge = TileToLength(topStrip, w, horizontal: true);
        using var botEdge = FlipV(topEdge);
        using var leftEdge = TileToLength(leftSeg, h, horizontal: false);
        using var rightEdge = FlipH(leftEdge);

        // 6. compose onto the enlarged canvas (painting stays untouched at T,T)
        int ow = w + 2 * t, oh = h + 2 * t;
        var canvas = new Image<Rgba32>(ow, oh); // already fully transparent

        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(painting, new Point(t, t), 1f);

            if (shadow) ApplyInnerShadow(ctx, t, w, h);

            ctx.DrawImage(topEdge, new Point(t, 0), 1f);
            ctx.DrawImage(botEdge, new Point(t, t + h), 1f);
            ctx.DrawImage(leftEdge, new Point(0, t), 1f);
            ctx.DrawImage(rightEdge, new Point(t + w, t), 1f);

            // corners last, so authentic ornament sits over the edge ends
            ctx.DrawImage(tl, new Point(0, 0), 1f);
            ctx.DrawImage(tr, new Point(t + w, 0), 1f);
            ctx.DrawImage(bl, new Point(0, t + h), 1f);
            ctx.DrawImage(br, new Point(t + w, t + h), 1f);
        });

        return canvas;
    }

    // ---- helpers ---------------------------------------------------------

    private static (int top, int left) DetectBands(Image<Rgba32> corner, byte alphaThresh)
    {
        int w = corner.Width, h = corner.Height;
        var alpha = new byte[h, w];
        corner.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                    alpha[y, x] = row[x].A;
            }
        });

        bool Op(int x, int y) => alpha[y, x] > alphaThresh;

        int TopRun(int x)
        {
            int r = 0;
            for (int y = 0; y < h; y++) { if (Op(x, y)) r = y + 1; else break; }
            return r;
        }
        int LeftRun(int y)
        {
            int r = 0;
            for (int x = 0; x < w; x++) { if (Op(x, y)) r = x + 1; else break; }
            return r;
        }

        var tops = new List<int>();
        for (int x = w / 2; x < w - 2; x++) tops.Add(TopRun(x));
        var lefts = new List<int>();
        for (int y = h / 2; y < h - 2; y++) lefts.Add(LeftRun(y));

        return (Median(tops), Median(lefts));
    }

    /// <summary>First strong autocorrelation peak of the straight top run = repeat length.</summary>
    private static int DetectPeriod(Image<Rgba32> norm, int t)
    {
        int w = norm.Width;
        int x0 = t, n = w - x0;
        if (n < 40) return 0;
        int y0 = 2, y1 = Math.Max(y0 + 1, t - 2);

        var sig = new double[n];
        norm.ProcessPixelRows(accessor =>
        {
            for (int i = 0; i < n; i++)
            {
                int x = x0 + i;
                double s = 0; int c = 0;
                for (int y = y0; y < y1; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    var p = row[x];
                    s += (p.R + p.G + p.B) / 3.0;
                    c++;
                }
                sig[i] = s / Math.Max(1, c);
            }
        });

        double mean = sig.Average();
        for (int i = 0; i < n; i++) sig[i] -= mean;

        double ac0 = 0;
        for (int i = 0; i < n; i++) ac0 += sig[i] * sig[i];
        if (ac0 == 0) return 0;

        double Ac(int lag)
        {
            double s = 0;
            for (int i = 0; i + lag < n; i++) s += sig[i] * sig[i + lag];
            return s / ac0;
        }

        int lo = 15, hi = Math.Min(180, n - 2);
        double prev = Ac(lo - 1), cur = Ac(lo), best = 0;
        int bestLag = 0;
        for (int lag = lo; lag < hi; lag++)
        {
            double next = Ac(lag + 1);
            if (cur > prev && cur > next && cur > best) { best = cur; bestLag = lag; }
            prev = cur; cur = next;
        }
        return bestLag;
    }

    /// <summary>Tile a strip (an integer number of periods) then micro-resize to fit exactly.</summary>
    private static Image<Rgba32> TileToLength(Image<Rgba32> strip, int length, bool horizontal)
    {
        int sw = strip.Width, sh = strip.Height;
        int seg = horizontal ? sw : sh;
        int reps = Math.Max(1, (int)Math.Round((double)length / seg));

        int tiledW = horizontal ? seg * reps : sw;
        int tiledH = horizontal ? sh : seg * reps;
        using var tiled = new Image<Rgba32>(tiledW, tiledH); // already fully transparent

        tiled.Mutate(ctx =>
        {
            for (int i = 0; i < reps; i++)
            {
                int px = horizontal ? i * seg : 0;
                int py = horizontal ? 0 : i * seg;
                ctx.DrawImage(strip, new Point(px, py), 1f);
            }
        });

        int targetW = horizontal ? length : sw;
        int targetH = horizontal ? sh : length;
        return Resize(tiled, targetW, targetH);
    }

    private static void ApplyInnerShadow(IImageProcessingContext ctx, int t, int w, int h)
    {
        int d = Math.Max(2, t / 18);
        for (int i = 0; i < d; i++)
        {
            byte a = (byte)(90 * (1 - (double)i / d));
            var color = Color.FromRgba(0, 0, 0, a);

            ctx.Fill(color, new RectangularPolygon(t, t + i, w, 1));
            ctx.Fill(color, new RectangularPolygon(t, t + h - 1 - i, w, 1));
            ctx.Fill(color, new RectangularPolygon(t + i, t, 1, h));
            ctx.Fill(color, new RectangularPolygon(t + w - 1 - i, t, 1, h));
        }
    }

    private static Image<Rgba32> Resize(Image<Rgba32> src, int w, int h) =>
        src.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(w, h),
            Sampler = KnownResamplers.Lanczos3
        }));

    private static Image<Rgba32> Crop(Image<Rgba32> src, int x, int y, int width, int height) =>
        src.Clone(ctx => ctx.Crop(new Rectangle(x, y, width, height)));

    private static Image<Rgba32> FlipH(Image<Rgba32> src) =>
        src.Clone(ctx => ctx.Flip(FlipMode.Horizontal));

    private static Image<Rgba32> FlipV(Image<Rgba32> src) =>
        src.Clone(ctx => ctx.Flip(FlipMode.Vertical));

    private static int Median(List<int> v)
    {
        if (v.Count == 0) return 0;
        v.Sort();
        return v[v.Count / 2];
    }
}
