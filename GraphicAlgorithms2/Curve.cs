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
    internal class Curve
    {
        public List<Point> ControlPoints { get; private set; }

        public Curve()
        {
            ControlPoints = new List<Point>();
        }

        public Curve(List<Point> controlPoints)
        {
            ControlPoints = controlPoints;
        }

        public void AddControlPoint(Point p)
        {
            ControlPoints.Add(p);
        }

        public Point CalculateBezierPoint(float t)
        {
            int n = ControlPoints.Count - 1;
            float x = 0, y = 0;
            for (int i = 0; i <= n; i++)
            {
                float binomialCoefficient = BinomialCoefficient(n, i);
                float term = binomialCoefficient * (float)Math.Pow(1 - t, n - i) * (float)Math.Pow(t, i);
                x += term * ControlPoints[i].X;
                y += term * ControlPoints[i].Y;
            }
            return new Point((int)x, (int)y);
        }

        private int BinomialCoefficient(int n, int k)
        {
            if (k > n) return 0;
            if (k == 0 || k == n) return 1;
            int res = 1;
            for (int i = 0; i < k; i++)
            {
                res *= (n - i);
                res /= (i + 1);
            }
            return res;
        }

        private void DrawControlPoints(PictureBox canvas, Bitmap bitmap, Color color)
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                foreach (var p in ControlPoints)
                {
                    g.FillEllipse(new SolidBrush(color), p.X - 3, p.Y - 3, 7, 7);
                    g.DrawEllipse(Pens.Black, p.X - 3, p.Y - 3, 7, 7);
                }
            }
            canvas.Invalidate();
        }

        public async Task DrawBezier(PictureBox canvas, Bitmap bitmap, Color color, int steps = 100)
        {
            if (ControlPoints.Count < 2) return;

            DrawControlPoints(canvas, bitmap, Color.Red);

            Point prev = CalculateBezierPoint(0f);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Point curr = CalculateBezierPoint(t);
                var line = new Line(prev, curr);
                await line.DrawDDA(canvas, bitmap, color, false, CancellationToken.None);
                prev = curr;
            }
            canvas.Invalidate();
        }


        public async Task DrawBSpline(PictureBox canvas, Bitmap bitmap, Color color, int steps = 100)
        {
            if (ControlPoints.Count < 4) return; // B-Spline cúbica requiere al menos 4 puntos

            DrawControlPoints(canvas, bitmap, Color.Red);

            for (int i = 0; i <= ControlPoints.Count - 4; i++)
            {
                Point prev = CalculateBSplinePoint(i, 0f);
                for (int j = 1; j <= steps; j++)
                {
                    float t = j / (float)steps;
                    Point curr = CalculateBSplinePoint(i, t);
                    var line = new Line(prev, curr);
                    await line.DrawDDA(canvas, bitmap, color, false, CancellationToken.None);
                    prev = curr;
                }
            }
            canvas.Invalidate();
        }

        private Point CalculateBSplinePoint(int i, float t)
        {
            // B-Spline cúbica uniforme
            float[] B = new float[4];
            B[0] = ((1 - t) * (1 - t) * (1 - t)) / 6f;
            B[1] = (3 * t * t * t - 6 * t * t + 4) / 6f;
            B[2] = (-3 * t * t * t + 3 * t * t + 3 * t + 1) / 6f;
            B[3] = (t * t * t) / 6f;

            float x = 0, y = 0;
            for (int k = 0; k < 4; k++)
            {
                x += B[k] * ControlPoints[i + k].X;
                y += B[k] * ControlPoints[i + k].Y;
            }
            return new Point((int)x, (int)y);
        }
    }
}
