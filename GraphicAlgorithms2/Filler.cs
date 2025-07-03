using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphicAlgorithms2
{
    internal class Filler
    {
        private Point Start { get; set; }

        public Filler(Point Start) {
            this.Start = Start;
        }

        public async Task FlowFill(PictureBox canvas, Bitmap bitmap, Color replacementColor, bool animationEnabled, CancellationToken token)
        {
            if (Start.X < 0 || Start.X >= bitmap.Width || Start.Y < 0 || Start.Y >= bitmap.Height)
                return;

            AnimationManager am = new AnimationManager("FLOWFILL ALGORITHM", animationEnabled, canvas, bitmap);

            Color targetColor = bitmap.GetPixel(Start.X, Start.Y);

            if (targetColor.ToArgb() == replacementColor.ToArgb())
                return;

            HashSet<Point> visited = new HashSet<Point>();
            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(Start);

            int count = 0;

            am.AlgorithmStart();

            while (pixels.Count > 0)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Algorithm cancelled.");
                    return;
                }

                Point p = pixels.Pop();

                if (p.X < 0 || p.X >= bitmap.Width || p.Y < 0 || p.Y >= bitmap.Height)
                    continue;

                if (visited.Contains(p))
                    continue;

                Color currentColor = bitmap.GetPixel(p.X, p.Y);
                if (currentColor.ToArgb() != targetColor.ToArgb())
                    continue;

                visited.Add(p);

                await am.SetPixel(p.X, p.Y, replacementColor, count++ % 100 == 0 ? 1 : 0);

                pixels.Push(new Point(p.X - 1, p.Y));
                pixels.Push(new Point(p.X, p.Y + 1));
                pixels.Push(new Point(p.X + 1, p.Y));
                pixels.Push(new Point(p.X, p.Y - 1));
            }

            canvas.Invalidate();
            am.AlgorithmEnd();
        }
        public async Task ScanlineFill(PictureBox canvas, Bitmap bitmap, Color replacementColor, bool animationEnabled, CancellationToken token)
        {
            if (Start.X < 0 || Start.X >= bitmap.Width || Start.Y < 0 || Start.Y >= bitmap.Height)
                return;

            AnimationManager am = new AnimationManager("SCANLINE FILL ALGORITHM", animationEnabled, canvas, bitmap);

            Color targetColor = bitmap.GetPixel(Start.X, Start.Y);

            if (targetColor.ToArgb() == replacementColor.ToArgb())
                return;

            Stack<(int x, int y, int direction)> scanlines = new Stack<(int x, int y, int direction)>();
            scanlines.Push((Start.X, Start.Y, 1));  // 1 = down, -1 = up

            int count = 0;

            am.AlgorithmStart();

            while (scanlines.Count > 0)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Algorithm cancelled.");
                    return;
                }

                var (x, y, direction) = scanlines.Pop();

                if (y < 0 || y >= bitmap.Height)
                    continue;

                if (bitmap.GetPixel(x, y).ToArgb() != targetColor.ToArgb())
                    continue;

                int left = x;
                while (left > 0 && bitmap.GetPixel(left - 1, y).ToArgb() == targetColor.ToArgb())
                    left--;

                int right = x;
                while (right < bitmap.Width - 1 && bitmap.GetPixel(right + 1, y).ToArgb() == targetColor.ToArgb())
                    right++;

                for (int i = left; i <= right; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Algorithm cancelled.");
                        return;
                    }

                    await am.SetPixel(i, y, replacementColor, count++ % 50 == 0 ? 1 : 0);
                }

                int nextY = y + direction;
                if (nextY >= 0 && nextY < bitmap.Height)
                {
                    ScanForSegments(left, right, nextY, direction, targetColor, scanlines, bitmap);
                }

                int oppositeY = y - direction;
                if (oppositeY >= 0 && oppositeY < bitmap.Height)
                {
                    ScanForSegments(left, right, oppositeY, -direction, targetColor, scanlines, bitmap);
                }
            }

            canvas.Invalidate();
            am.AlgorithmEnd();
        }

        private void ScanForSegments(int left, int right, int y, int direction, Color targetColor,
            Stack<(int x, int y, int direction)> scanlines, Bitmap bitmap)
        {
            bool inSegment = false;
            int segmentStart = 0;

            for (int x = left; x <= right; x++)
            {
                bool isTarget = bitmap.GetPixel(x, y).ToArgb() == targetColor.ToArgb();

                if (!inSegment && isTarget)
                {
                    inSegment = true;
                    segmentStart = x;
                }
                else if (inSegment && !isTarget)
                {
                    scanlines.Push((segmentStart, y, direction));
                    inSegment = false;
                }
            }

            if (inSegment)
            {
                scanlines.Push((segmentStart, y, direction));
            }
        }
    }
}
