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
    internal class Clipper
    {
        private Rectangle clippingArea;

        private const int INSIDE = 0; // 0000
        private const int LEFT = 1;   // 0001
        private const int RIGHT = 2;  // 0010
        private const int BOTTOM = 4; // 0100
        private const int TOP = 8;    // 1000

        public Clipper(Rectangle clippingArea)
        {
            this.clippingArea = clippingArea;
        }

        public Line CohenSutherlandClip(Line line)
        {
            Point p1 = line.Start;
            Point p2 = line.End;

            int x1 = p1.X, y1 = p1.Y;
            int x2 = p2.X, y2 = p2.Y;

            int outcode1 = ComputeOutCode(x1, y1);
            int outcode2 = ComputeOutCode(x2, y2);

            bool accept = false;

            while (true)
            {
                if ((outcode1 | outcode2) == 0)
                {
                    accept = true;
                    break;
                }
                else if ((outcode1 & outcode2) != 0)
                {
                    break;
                }
                else
                {
                    int x = 0, y = 0;
                    int outcodeOut = (outcode1 != 0) ? outcode1 : outcode2;

                    if ((outcodeOut & TOP) != 0)
                    {
                        // Punto está arriba del área de recorte
                        x = x1 + (x2 - x1) * (clippingArea.Top - y1) / (y2 - y1);
                        y = clippingArea.Top;
                    }
                    else if ((outcodeOut & BOTTOM) != 0)
                    {
                        // Punto está debajo del área de recorte
                        x = x1 + (x2 - x1) * (clippingArea.Bottom - y1) / (y2 - y1);
                        y = clippingArea.Bottom;
                    }
                    else if ((outcodeOut & RIGHT) != 0)
                    {
                        // Punto está a la derecha del área de recorte
                        y = y1 + (y2 - y1) * (clippingArea.Right - x1) / (x2 - x1);
                        x = clippingArea.Right;
                    }
                    else if ((outcodeOut & LEFT) != 0)
                    {
                        // Punto está a la izquierda del área de recorte
                        y = y1 + (y2 - y1) * (clippingArea.Left - x1) / (x2 - x1);
                        x = clippingArea.Left;
                    }

                    if (outcodeOut == outcode1)
                    {
                        x1 = x;
                        y1 = y;
                        outcode1 = ComputeOutCode(x1, y1);
                    }
                    else
                    {
                        x2 = x;
                        y2 = y;
                        outcode2 = ComputeOutCode(x2, y2);
                    }
                }
            }

            if (accept)
            {
                return new Line(new Point(x1, y1), new Point(x2, y2));
            }
            else
            {
                return null;
            }
        }

        private int ComputeOutCode(int x, int y)
        {
            int code = INSIDE;

            if (x < clippingArea.Left)           // A la izquierda del rectángulo
                code |= LEFT;
            else if (x > clippingArea.Right)     // A la derecha del rectángulo
                code |= RIGHT;

            if (y < clippingArea.Top)            // Arriba del rectángulo
                code |= TOP;
            else if (y > clippingArea.Bottom)    // Debajo del rectángulo
                code |= BOTTOM;

            return code;
        }

        public Polygon SutherlandHodgmanClip(Polygon polygon)
        {
            List<Point> outputList = new List<Point>(polygon.Points);

            outputList = ClipAgainstEdge(outputList, clippingArea.Left, EdgeType.Left);
            outputList = ClipAgainstEdge(outputList, clippingArea.Right, EdgeType.Right);
            outputList = ClipAgainstEdge(outputList, clippingArea.Top, EdgeType.Top);
            outputList = ClipAgainstEdge(outputList, clippingArea.Bottom, EdgeType.Bottom);

            return new Polygon(outputList);
        }

        private enum EdgeType { Left, Right, Top, Bottom }

        private List<Point> ClipAgainstEdge(List<Point> inputList, int edgePosition, EdgeType edgeType)
        {
            if (inputList.Count == 0) return inputList;

            List<Point> outputList = new List<Point>();

            if (inputList.Count > 0)
            {
                Point s = inputList[inputList.Count - 1];

                foreach (Point e in inputList)
                {
                    if (IsInside(e, edgePosition, edgeType))
                    {
                        if (!IsInside(s, edgePosition, edgeType))
                        {
                            Point intersection = ComputeIntersection(s, e, edgePosition, edgeType);
                            outputList.Add(intersection);
                        }
                        outputList.Add(e);
                    }
                    else if (IsInside(s, edgePosition, edgeType))
                    {
                        Point intersection = ComputeIntersection(s, e, edgePosition, edgeType);
                        outputList.Add(intersection);
                    }

                    s = e;
                }
            }

            return outputList;
        }

        private bool IsInside(Point point, int edgePosition, EdgeType edgeType)
        {
            switch (edgeType)
            {
                case EdgeType.Left:
                    return point.X >= edgePosition;
                case EdgeType.Right:
                    return point.X <= edgePosition;
                case EdgeType.Top:
                    return point.Y >= edgePosition;
                case EdgeType.Bottom:
                    return point.Y <= edgePosition;
                default:
                    return false;
            }
        }

        private Point ComputeIntersection(Point p1, Point p2, int edgePosition, EdgeType edgeType)
        {
            int x, y;

            switch (edgeType)
            {
                case EdgeType.Left:
                case EdgeType.Right:
                    x = edgePosition;
                    y = p1.Y + (p2.Y - p1.Y) * (edgePosition - p1.X) / (p2.X - p1.X);
                    break;
                case EdgeType.Top:
                case EdgeType.Bottom:
                    y = edgePosition;
                    x = p1.X + (p2.X - p1.X) * (edgePosition - p1.Y) / (p2.Y - p1.Y);
                    break;
                default:
                    return p1;
            }

            return new Point(x, y);
        }

        public async Task DrawClippingArea(PictureBox canvas, Bitmap bitmap, Color color, CancellationToken token)
        {
            AnimationManager am = new AnimationManager("DRAWING CLIPPING AREA", false, canvas, bitmap);

            am.AlgorithmStart();

            await DrawRectangleEdges(am, color, token);

            canvas.Invalidate();
            am.AlgorithmEnd();
        }

        private async Task DrawRectangleEdges(AnimationManager am, Color color, CancellationToken token)
        {
            Line line1 = new Line(new Point(clippingArea.Left, clippingArea.Top), new Point(clippingArea.Right, clippingArea.Top));
            Line line2 = new Line(new Point(clippingArea.Right, clippingArea.Top), new Point(clippingArea.Right, clippingArea.Bottom));
            Line line3 = new Line(new Point(clippingArea.Right, clippingArea.Bottom), new Point(clippingArea.Left, clippingArea.Bottom));
            Line line4 = new Line(new Point(clippingArea.Left, clippingArea.Bottom), new Point(clippingArea.Left, clippingArea.Top));
            await line1.DrawDDA(am.canvas, am.bitmap, color, am.AnimationEnabled, token);
            await line2.DrawDDA(am.canvas, am.bitmap, color, am.AnimationEnabled, token);
            await line3.DrawDDA(am.canvas, am.bitmap, color, am.AnimationEnabled, token);
            await line4.DrawDDA(am.canvas, am.bitmap, color, am.AnimationEnabled, token);
        }

        public Rectangle GetClippingArea()
        {
            return clippingArea;
        }

        public void SetClippingArea(Rectangle newArea)
        {
            clippingArea = newArea;
        }

        public bool IsPointInside(Point point)
        {
            return clippingArea.Contains(point);
        }

        public bool IsLineCompletelyInside(Line line)
        {
            return IsPointInside(line.Start) && IsPointInside(line.End);
        }

        public bool IsLineCompletelyOutside(Line line)
        {
            int outcode1 = ComputeOutCode(line.Start.X, line.Start.Y);
            int outcode2 = ComputeOutCode(line.End.X, line.End.Y);

            return (outcode1 & outcode2) != 0;
        }
    }
}
