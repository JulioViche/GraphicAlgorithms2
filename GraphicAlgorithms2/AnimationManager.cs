using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphicAlgorithms2
{
    internal class AnimationManager
    {
        public string AlgorithmName;
        public bool AnimationEnabled;
        public PictureBox canvas;
        public Bitmap bitmap;

        public AnimationManager(string algorithmName, bool animationEnabled, PictureBox canvas, Bitmap bitmap)
        {
            AlgorithmName = algorithmName;
            AnimationEnabled = animationEnabled;
            this.canvas = canvas;
            this.bitmap = bitmap;
        }

        public void AlgorithmStart()
        {
            if (AnimationEnabled)
            {
                Console.WriteLine("------------------------------");
                Console.WriteLine($"{AlgorithmName}:");
                Console.WriteLine("------------------------------");
            }
        }

        public void AlgorithmEnd()
        {
            if (AnimationEnabled) Console.WriteLine($"Algorithm finished!\n");
        }

        public async Task SetPixel(int x, int y, Color color, int delay)
        {
            bitmap.SetPixel(x, y, color);

            if (AnimationEnabled)
            {
                canvas.Invalidate();
                Console.WriteLine($"  Pixel ({x}, {y})");
                await Task.Delay(delay);
            }
        }
    }
}
