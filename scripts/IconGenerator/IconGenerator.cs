using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: IconGenerator <out.ico> [out-preview.png]");
            return 1;
        }

        int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
        Bitmap[] bitmaps = new Bitmap[sizes.Length];
        try
        {
            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = DrawIcon(sizes[i]);
            }

            SaveMultiSizeIcon(args[0], bitmaps);
            Console.WriteLine("Wrote " + args[0]);

            // Optional sharp 256px PNG preview (do not upscale from the tiny default icon frame).
            if (args.Length >= 2)
            {
                using (Bitmap preview = DrawIcon(256))
                {
                    preview.Save(args[1], ImageFormat.Png);
                }
                Console.WriteLine("Wrote " + args[1]);
            }

            return 0;
        }
        finally
        {
            for (int i = 0; i < bitmaps.Length; i++)
            {
                if (bitmaps[i] != null)
                {
                    bitmaps[i].Dispose();
                }
            }
        }
    }

    private static float Scale(float value, int size)
    {
        return value * size / 256f;
    }

    private static Bitmap DrawIcon(int size)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            using (GraphicsPath bg = RoundedRect(
                Scale(8, size), Scale(8, size), Scale(240, size), Scale(240, size), Scale(48, size)))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                new RectangleF(Scale(8, size), Scale(8, size), Scale(240, size), Scale(240, size)),
                Color.FromArgb(255, 27, 110, 243),
                Color.FromArgb(255, 14, 70, 180),
                135f))
            {
                g.FillPath(brush, bg);
            }

            using (GraphicsPath sheen = RoundedRect(
                Scale(20, size), Scale(20, size), Scale(216, size), Scale(100, size), Scale(36, size)))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                new RectangleF(Scale(20, size), Scale(20, size), Scale(216, size), Scale(100, size)),
                Color.FromArgb(55, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                90f))
            {
                g.FillPath(brush, sheen);
            }

            // Windows-style client window (mild corner radius, caption buttons on the right)
            float wx = Scale(52, size);
            float wy = Scale(62, size);
            float ww = Scale(152, size);
            float wh = Scale(124, size);
            float corner = Scale(8, size);
            float titleH = Scale(30, size);

            using (GraphicsPath window = RoundedRect(wx, wy, ww, wh, corner))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(255, 243, 243, 243)))
            using (Pen border = new Pen(Color.FromArgb(255, 120, 120, 120), Math.Max(1f, Scale(2.5f, size))))
            {
                g.FillPath(fill, window);
                g.DrawPath(border, window);
            }

            // Title bar (Windows light chrome)
            using (GraphicsPath title = RoundedRect(wx, wy, ww, titleH, corner))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
            {
                g.FillPath(brush, title);
                g.FillRectangle(brush, wx, wy + corner, ww, titleH - corner);
            }

            // Title bar bottom edge
            using (Pen edge = new Pen(Color.FromArgb(255, 200, 200, 200), Math.Max(1f, Scale(1.5f, size))))
            {
                g.DrawLine(edge, wx + Scale(1, size), wy + titleH, wx + ww - Scale(1, size), wy + titleH);
            }

            // Caption buttons: minimize / maximize / close (right side, Windows order)
            // (No left-side blue app glyph — it sat under the pin tip and looked wrong.)
            if (size >= 24)
            {
                DrawWindowsCaptionButtons(g, wx, wy, ww, titleH, size);
            }

            // Map-style pin marking the window origin (top-left)
            float pinTipX = wx + Scale(4, size);
            float pinTipY = wy + Scale(4, size);
            DrawMapPin(g, pinTipX, pinTipY, size);

            if (size >= 32)
            {
                using (Pen pen = new Pen(Color.FromArgb(230, 255, 255, 255), Math.Max(2f, Scale(7, size))))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.CustomEndCap = new AdjustableArrowCap(3f, 3.5f, true);
                    g.DrawLine(
                        pen,
                        wx + ww * 0.72f,
                        wy + wh * 0.72f,
                        wx + Scale(18, size),
                        wy + Scale(36, size));
                }
            }
        }

        return bmp;
    }

    /// <summary>
    /// Draws a solid yellow map pin whose tip sits at (tipX, tipY).
    /// One outer silhouette only — no internal V lines from a separate triangle stroke.
    /// GDI+ angles: 0=east, 90=south, 180=west, 270=north (clockwise).
    /// </summary>
    private static void DrawMapPin(Graphics g, float tipX, float tipY, int size)
    {
        float headR = Scale(18, size);
        float headCx = tipX;
        float headCy = tipY - headR * 1.25f;
        float tipY2 = tipY + Scale(2, size);

        if (size >= 32)
        {
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                float sw = Scale(14, size);
                float sh = Scale(6, size);
                g.FillEllipse(shadow, tipX - sw / 2f, tipY + Scale(1, size), sw, sh);
            }
        }

        Color yellow = Color.FromArgb(255, 255, 204, 0);
        Color outlineColor = Color.FromArgb(255, 40, 50, 80);
        float stroke = Math.Max(1f, Scale(2.5f, size));

        // Join angles on the circle (bottom-left / bottom-right), arc goes the long way through the top.
        const float leftAngle = 135f;
        const float rightAngle = 45f;
        // Clockwise from 135° to 45°: 135 -> 180 -> 270 -> 360 -> 45 = 270°
        const float startAngle = leftAngle;
        const float sweepAngle = 270f;

        using (GraphicsPath pin = new GraphicsPath())
        {
            pin.FillMode = FillMode.Winding;
            pin.AddArc(
                headCx - headR,
                headCy - headR,
                headR * 2f,
                headR * 2f,
                startAngle,
                sweepAngle);

            // Arc ends at rightAngle (45°). Then tip, then close back to left join.
            float endX = headCx + headR * (float)Math.Cos(rightAngle * Math.PI / 180.0);
            float endY = headCy + headR * (float)Math.Sin(rightAngle * Math.PI / 180.0);
            float startX = headCx + headR * (float)Math.Cos(leftAngle * Math.PI / 180.0);
            float startY = headCy + headR * (float)Math.Sin(leftAngle * Math.PI / 180.0);

            pin.AddLine(endX, endY, tipX, tipY2);
            pin.AddLine(tipX, tipY2, startX, startY);
            pin.CloseFigure();

            using (SolidBrush fill = new SolidBrush(yellow))
            using (Pen outline = new Pen(outlineColor, stroke))
            {
                outline.LineJoin = LineJoin.Round;
                outline.StartCap = LineCap.Round;
                outline.EndCap = LineCap.Round;
                g.FillPath(fill, pin);
                g.DrawPath(outline, pin);
            }
        }

        // Dark center hole only
        float holeR = Math.Max(1.5f, headR * 0.32f);
        using (SolidBrush hole = new SolidBrush(Color.FromArgb(255, 30, 40, 70)))
        {
            g.FillEllipse(hole, headCx - holeR, headCy - holeR, holeR * 2f, holeR * 2f);
        }
    }

    private static void DrawWindowsCaptionButtons(
        Graphics g, float wx, float wy, float ww, float titleH, int size)
    {
        float btnW = Scale(22, size);
        float gap = Scale(2, size);
        float total = btnW * 3 + gap * 2;
        float rightPad = Scale(8, size);
        float x0 = wx + ww - rightPad - total;
        float cy = wy + titleH / 2f;
        float stroke = Math.Max(1.2f, Scale(2.2f, size));
        Color glyph = Color.FromArgb(255, 50, 50, 50);

        using (Pen pen = new Pen(glyph, stroke))
        {
            pen.StartCap = LineCap.Flat;
            pen.EndCap = LineCap.Flat;

            // Minimize —
            float minCx = x0 + btnW / 2f;
            float half = Scale(6, size);
            g.DrawLine(pen, minCx - half, cy, minCx + half, cy);

            // Maximize □
            float maxCx = x0 + btnW + gap + btnW / 2f;
            float box = Scale(9, size);
            g.DrawRectangle(pen, maxCx - box / 2f, cy - box / 2f, box, box);

            // Close ×
            float closeCx = x0 + 2 * (btnW + gap) + btnW / 2f;
            float arm = Scale(5.5f, size);
            g.DrawLine(pen, closeCx - arm, cy - arm, closeCx + arm, cy + arm);
            g.DrawLine(pen, closeCx + arm, cy - arm, closeCx - arm, cy + arm);
        }
    }

    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float d = radius * 2;
        if (d > w) d = w;
        if (d > h) d = h;
        if (d < 0.1f)
        {
            path.AddRectangle(new RectangleF(x, y, w, h));
            return path;
        }
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void SaveMultiSizeIcon(string path, Bitmap[] bitmaps)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)bitmaps.Length);

            byte[][] imageData = new byte[bitmaps.Length][];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                imageData[i] = EncodePng(bitmaps[i]);
            }

            int offset = 6 + (16 * bitmaps.Length);
            for (int i = 0; i < bitmaps.Length; i++)
            {
                int size = bitmaps[i].Width;
                bw.Write((byte)(size >= 256 ? 0 : size));
                bw.Write((byte)(size >= 256 ? 0 : size));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(imageData[i].Length);
                bw.Write(offset);
                offset += imageData[i].Length;
            }

            for (int i = 0; i < bitmaps.Length; i++)
            {
                bw.Write(imageData[i]);
            }

            File.WriteAllBytes(path, ms.ToArray());
        }
    }

    private static byte[] EncodePng(Bitmap bmp)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }
}
