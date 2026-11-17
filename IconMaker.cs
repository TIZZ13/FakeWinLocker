using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

internal static class IconMaker
{
    private sealed class IconImage
    {
        public int Size;
        public byte[] Data;
    }

    private static void Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : "app.ico";

        using (Bitmap canvas = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                DrawIcon(g);
            }

            canvas.Save("icon_preview.png", ImageFormat.Png);

            int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
            List<IconImage> images = new List<IconImage>();
            foreach (int size in sizes)
            {
                using (Bitmap resized = Resize(canvas, size))
                using (MemoryStream ms = new MemoryStream())
                {
                    resized.Save(ms, ImageFormat.Png);
                    IconImage image = new IconImage();
                    image.Size = size;
                    image.Data = ms.ToArray();
                    images.Add(image);
                }
            }

            WriteIco(output, images);
        }

        Console.WriteLine("OK: " + output);
    }

    private static void DrawIcon(Graphics g)
    {
        Rectangle full = new Rectangle(0, 0, 1024, 1024);

        using (LinearGradientBrush back = new LinearGradientBrush(
            full,
            Color.FromArgb(255, 3, 1, 10),
            Color.FromArgb(255, 10, 4, 28),
            90.0f))
        {
            g.FillRectangle(back, full);
        }

        using (GraphicsPath nebulaPath = new GraphicsPath())
        {
            nebulaPath.AddEllipse(150, 130, 724, 724);
            using (PathGradientBrush nebula = new PathGradientBrush(nebulaPath))
            {
                nebula.CenterColor = Color.FromArgb(105, 74, 24, 178);
                nebula.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                nebula.CenterPoint = new PointF(512, 500);
                nebula.FocusScales = new PointF(0.35f, 0.35f);
                g.FillPath(nebula, nebulaPath);
            }
        }

        Random random = new Random(3015);
        for (int i = 0; i < 210; i++)
        {
            float x = random.Next(8, 1016);
            float y = random.Next(8, 1016);
            float radius = 0.7f + (float)random.NextDouble() * 2.4f;
            int alpha = 40 + random.Next(0, 200);
            using (SolidBrush star = new SolidBrush(Color.FromArgb(alpha, 218, 198, 255)))
            {
                g.FillEllipse(star, x, y, radius, radius);
            }
        }

        DrawSparkle(g, 806, 510, 58, 235);
        DrawSparkle(g, 246, 764, 28, 210);
        DrawSparkle(g, 850, 206, 17, 180);
        DrawSparkle(g, 170, 270, 12, 150);

        DrawOrbit(g);

        using (GraphicsPath sPath = BuildSPath())
        {
            DrawSGlow(g, sPath);
            DrawSFill(g, sPath);
            DrawSGlass(g, sPath);
            DrawSOutline(g, sPath);
        }
    }

    private static GraphicsPath BuildSPath()
    {
        FontFamily family = null;
        string[] candidates = new string[] { "Segoe UI Black", "Arial Black", "Impact", "Segoe UI" };
        foreach (string candidate in candidates)
        {
            try
            {
                family = new FontFamily(candidate);
                break;
            }
            catch
            {
            }
        }

        if (family == null)
        {
            family = FontFamily.GenericSansSerif;
        }

        GraphicsPath path = new GraphicsPath();
        path.AddString(
            "S",
            family,
            (int)FontStyle.Bold,
            900.0f,
            new PointF(0, 0),
            StringFormat.GenericTypographic);

        RectangleF bounds = path.GetBounds();
        Matrix transform = new Matrix();
        transform.Translate(-bounds.X - bounds.Width * 0.5f, -bounds.Y - bounds.Height * 0.5f);
        transform.Scale(500.0f / bounds.Width, 650.0f / bounds.Height);
        transform.Translate(500.0f, 486.0f);
        path.Transform(transform);
        return path;
    }

    private static void DrawSGlow(Graphics g, GraphicsPath path)
    {
        Color glow = Color.FromArgb(255, 120, 45, 255);
        float[] widths = new float[] { 96.0f, 68.0f, 44.0f, 26.0f, 14.0f };
        int[] alphas = new int[] { 14, 26, 48, 82, 130 };

        for (int i = 0; i < widths.Length; i++)
        {
            using (Pen pen = new Pen(Color.FromArgb(alphas[i], glow), widths[i]))
            {
                pen.LineJoin = LineJoin.Round;
                g.DrawPath(pen, path);
            }
        }
    }

    private static void DrawSFill(Graphics g, GraphicsPath path)
    {
        using (LinearGradientBrush fill = new LinearGradientBrush(
            new PointF(330, 150),
            new PointF(700, 900),
            Color.White,
            Color.Black))
        {
            ColorBlend blend = new ColorBlend();
            blend.Positions = new float[] { 0.0f, 0.18f, 0.42f, 0.68f, 0.86f, 1.0f };
            blend.Colors = new Color[]
            {
                Color.FromArgb(255, 232, 196, 255),
                Color.FromArgb(255, 185, 110, 255),
                Color.FromArgb(255, 137, 48, 250),
                Color.FromArgb(255, 94, 19, 214),
                Color.FromArgb(255, 57, 6, 150),
                Color.FromArgb(255, 28, 1, 80)
            };
            fill.InterpolationColors = blend;
            g.FillPath(fill, path);
        }
    }

    private static void DrawSGlass(Graphics g, GraphicsPath path)
    {
        GraphicsState state = g.Save();
        g.SetClip(path);

        using (LinearGradientBrush highlight = new LinearGradientBrush(
            new PointF(0, 0),
            new PointF(0, 1024),
            Color.FromArgb(88, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255)))
        {
            g.FillRectangle(highlight, 0, 0, 1024, 1024);
        }

        using (LinearGradientBrush shade = new LinearGradientBrush(
            new PointF(0, 500),
            new PointF(0, 1024),
            Color.FromArgb(0, 16, 0, 50),
            Color.FromArgb(125, 16, 0, 50)))
        {
            g.FillRectangle(shade, 0, 0, 1024, 1024);
        }

        Random random = new Random(777);
        for (int i = 0; i < 34; i++)
        {
            float x = random.Next(220, 820);
            float y = random.Next(160, 860);
            float radius = 1.0f + (float)random.NextDouble() * 3.0f;
            int alpha = 90 + random.Next(0, 150);
            using (SolidBrush star = new SolidBrush(Color.FromArgb(alpha, 255, 250, 255)))
            {
                g.FillEllipse(star, x, y, radius, radius);
            }
        }

        using (Pen streak = new Pen(Color.FromArgb(42, 255, 255, 255), 13.0f))
        {
            g.DrawLine(streak, 300, 800, 625, 330);
            g.DrawLine(streak, 390, 840, 690, 405);
        }

        using (SolidBrush glint = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
        {
            g.FillEllipse(glint, 402, 300, 20, 13);
            g.FillEllipse(glint, 610, 270, 14, 9);
            g.FillEllipse(glint, 452, 744, 16, 10);
        }

        g.Restore(state);
    }

    private static void DrawSOutline(Graphics g, GraphicsPath path)
    {
        using (Pen outer = new Pen(Color.FromArgb(150, 199, 138, 255), 10.0f))
        {
            outer.LineJoin = LineJoin.Round;
            g.DrawPath(outer, path);
        }

        using (Pen inner = new Pen(Color.FromArgb(235, 244, 228, 255), 3.2f))
        {
            inner.LineJoin = LineJoin.Round;
            g.DrawPath(inner, path);
        }
    }

    private static void DrawOrbit(Graphics g)
    {
        GraphicsState state = g.Save();
        g.TranslateTransform(510.0f, 512.0f);
        g.RotateTransform(-19.0f);

        RectangleF orbit = new RectangleF(-402, -158, 804, 316);

        using (Pen glow = new Pen(Color.FromArgb(55, 137, 61, 255), 30.0f))
        {
            g.DrawEllipse(glow, orbit);
        }

        using (Pen line = new Pen(Color.FromArgb(155, 154, 99, 255), 8.0f))
        {
            g.DrawEllipse(line, orbit);
        }

        using (Pen core = new Pen(Color.FromArgb(210, 238, 220, 255), 2.5f))
        {
            g.DrawEllipse(core, orbit);
        }

        g.Restore(state);
    }

    private static void DrawSparkle(Graphics g, float cx, float cy, float size, int alpha)
    {
        using (GraphicsPath path = new GraphicsPath())
        {
            float s = size;
            path.StartFigure();
            path.AddBezier(cx, cy - s, cx + s * 0.08f, cy - s * 0.18f, cx + s * 0.18f, cy - s * 0.08f, cx + s, cy);
            path.AddBezier(cx + s, cy, cx + s * 0.18f, cy + s * 0.08f, cx + s * 0.08f, cy + s * 0.18f, cx, cy + s);
            path.AddBezier(cx, cy + s, cx - s * 0.08f, cy + s * 0.18f, cx - s * 0.18f, cy + s * 0.08f, cx - s, cy);
            path.AddBezier(cx - s, cy, cx - s * 0.18f, cy - s * 0.08f, cx - s * 0.08f, cy - s * 0.18f, cx, cy - s);
            path.CloseFigure();

            using (Pen glow = new Pen(Color.FromArgb(alpha / 4, 180, 100, 255), s * 0.30f))
            {
                glow.LineJoin = LineJoin.Round;
                g.DrawPath(glow, path);
            }

            using (SolidBrush star = new SolidBrush(Color.FromArgb(alpha, 250, 244, 255)))
            {
                g.FillPath(star, path);
            }
        }
    }

    private static Bitmap Resize(Bitmap source, int size)
    {
        Bitmap target = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(target))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, size, size));
        }
        return target;
    }

    private static void WriteIco(string path, List<IconImage> images)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Count);

            int offset = 6 + 16 * images.Count;
            foreach (IconImage image in images)
            {
                writer.Write((byte)(image.Size >= 256 ? 0 : image.Size));
                writer.Write((byte)(image.Size >= 256 ? 0 : image.Size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(image.Data.Length);
                writer.Write(offset);
                offset += image.Data.Length;
            }

            foreach (IconImage image in images)
            {
                writer.Write(image.Data);
            }
        }
    }
}
