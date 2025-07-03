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
    internal class Line
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        private int Dx => End.X - Start.X;
        private int Dy => End.Y - Start.Y;

        public Line(Point start, Point end)
        {
            this.Start = start;
            this.End = end;
        }

        public async Task DrawDDA(PictureBox canvas, Bitmap bitmap, Color color, bool animationEnabled, CancellationToken token)
        {
            AnimationManager am = new AnimationManager("DDA LINE", animationEnabled, canvas, bitmap);

            int steps = Math.Max(Math.Abs(Dx), Math.Abs(Dy));
            float xInc = (float)Dx / steps;
            float yInc = (float)Dy / steps;

            float xk = Start.X;
            float yk = Start.Y;

            am.AlgorithmStart();

            for (int i = 0; i <= steps; i++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Algorithm cancelled.");
                    return;
                }

                await am.SetPixel((int)Math.Round(xk), (int)Math.Round(yk), color, 2);

                xk += xInc;
                yk += yInc;
            }

            canvas.Invalidate();
            am.AlgorithmEnd();
        }

        public async Task DrawBresenham(PictureBox canvas, Bitmap bitmap, Color color, bool animationEnabled, CancellationToken token)
        {
            AnimationManager am = new AnimationManager("BRESENHAM LINE", animationEnabled, canvas, bitmap);

            int x0 = Start.X;
            int y0 = Start.Y;
            int x1 = End.X;
            int y1 = End.Y;

            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (steep)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }

            int dx = x1 - x0;
            int dy = Math.Abs(y1 - y0);
            int error = dx / 2;
            int ystep = y0 < y1 ? 1 : -1;
            int y = y0;

            am.AlgorithmStart();

            for (int x = x0; x <= x1; x++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Algorithm cancelled.");
                    return;
                }

                int drawX = steep ? y : x;
                int drawY = steep ? x : y;

                await am.SetPixel(drawX, drawY, color, 2);

                error -= dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }

            canvas.Invalidate();
            am.AlgorithmEnd();
        }
    }
}
