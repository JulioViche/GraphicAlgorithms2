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
    internal class Ellipse
    {
        protected Point Center { get; set; }
        private int SemiaxisX { get; set; }
        private int SemiaxisY { get; set; }

        public Ellipse (Point center, int semiaxisX, int semiaxisY)
        {
            Center = center;
            SemiaxisX = semiaxisX;
            SemiaxisY = semiaxisY;
        }

        public virtual async Task DrawBresenham(PictureBox canvas, Bitmap bitmap, Color color, bool animationEnabled, CancellationToken token)
        {
            AnimationManager am = new AnimationManager("BRESENHAM ELLIPSE", animationEnabled, canvas, bitmap);

            int xc = Center.X;
            int yc = Center.Y;
            int a = SemiaxisX;
            int b = SemiaxisY;

            int x = 0;
            int y = b;
            int a2 = a * a;
            int b2 = b * b;
            int d1 = b2 - a2 * b + a2 / 4;
            int dx = 2 * b2 * x;
            int dy = 2 * a2 * y;

            am.AlgorithmStart();

            while (dx < dy)
            {
                if (token.IsCancellationRequested)
                    return;

                await am.SetPixel(xc + x, yc + y, Color.Black, 1);
                await am.SetPixel(xc - x, yc + y, Color.Black, 0);
                await am.SetPixel(xc + x, yc - y, Color.Black, 0);
                await am.SetPixel(xc - x, yc - y, Color.Black, 0);

                if (d1 < 0)
                {
                    x++;
                    dx = dx + (2 * b2);
                    d1 = d1 + dx + b2;
                }
                else
                {
                    x++;
                    y--;
                    dx = dx + (2 * b2);
                    dy = dy - (2 * a2);
                    d1 = d1 + dx - dy + b2;
                }
            }

            int d2 = (int)(b2 * (x + 0.5) * (x + 0.5) + a2 * (y - 1) * (y - 1) - a2 * b2);

            while (y >= 0)
            {
                if (token.IsCancellationRequested)
                    return;

                await am.SetPixel(xc + x, yc + y, color, 1);
                await am.SetPixel(xc - x, yc + y, color, 0);
                await am.SetPixel(xc + x, yc - y, color, 0);
                await am.SetPixel(xc - x, yc - y, color, 0);

                if (d2 > 0)
                {
                    y--;
                    dy = dy - (2 * a2);
                    d2 = d2 + a2 - dy;
                }
                else
                {
                    y--;
                    x++;
                    dx = dx + (2 * b2);
                    dy = dy - (2 * a2);
                    d2 = d2 + dx - dy + a2;
                }
            }

            canvas.Invalidate();
            am.AlgorithmEnd();
        }
    }
}
