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
    internal class Circle : Ellipse
    {
        private int Radius { get; set; }
        public Circle(Point center, int radius) : base(center, radius, radius)
        {
            Radius = radius;
        }

        public override async Task DrawBresenham(PictureBox canvas, Bitmap bitmap, Color color, bool animationEnabled, CancellationToken token)
        {
            AnimationManager am = new AnimationManager("BRESENHAM CIRCLE", animationEnabled, canvas, bitmap);

            int x = 0;
            int y = Radius;
            int d = 3 - 2 * Radius;

            am.AlgorithmStart();

            while (x <= y)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Algorithm cancelled.");
                    return;
                }

                int xc = Center.X;
                int yc = Center.Y;

                await am.SetPixel(xc + x, yc + y, color, 1);
                await am.SetPixel(xc - x, yc + y, color, 0);
                await am.SetPixel(xc + x, yc - y, color, 0);
                await am.SetPixel(xc - x, yc - y, color, 0);
                await am.SetPixel(xc + y, yc + x, color, 0);
                await am.SetPixel(xc - y, yc + x, color, 0);
                await am.SetPixel(xc + y, yc - x, color, 0);
                await am.SetPixel(xc - y, yc - x, color, 0);

                if (d < 0)
                {
                    d += 4 * x + 6;
                }
                else
                {
                    d += 4 * (x - y) + 10;
                    y--;
                }
                x++;
            }

            canvas.Invalidate();
            am.AlgorithmEnd();
        }
    }
}
