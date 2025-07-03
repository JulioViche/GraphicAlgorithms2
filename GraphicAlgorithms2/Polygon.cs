using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace GraphicAlgorithms2
{
    internal class Polygon
    {
        public List<Point> Points;

        public Polygon()
        {
            Points = new List<Point>();
        }

        public Polygon(List<Point> points)
        {
            this.Points = points;
        }

        public Polygon(Point[] points)
        {
            this.Points = new List<Point>(points);
        }

        public void AddVertice(Point p)
        {
            Points.Add(p);
        }

        public void Draw(PictureBox canvas, Bitmap bitmap, Color color)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Point start = Points[i];
                Point end = Points[(i + 1) % Points.Count];
                Line line = new Line(start, end);
                line.DrawDDA(canvas, bitmap, color, false, new CancellationToken());
            }
        }
    }
}
