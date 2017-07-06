using System.Windows;
using System.Windows.Media;

namespace Bubble_Bash
{
    public class Bubble
    {
        //public static Brush RedBrush = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        //public static Brush GreenBrush = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
        //public static Brush BlueBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));

        private Color color;
        public Color BubbleColor
        {
            get { return color; }
            set { color = value; }
        }

        private Point position;
        public Point BubblePosition
        {
            get { return position; }
            set { position = value; }
        }

        private double size;
        public double BubbleSize
        {
            get { return size; }
            set { size = value; }
        }

        private int timeToDissappear;
        public int TimeToDisappear
        {
            get { return timeToDissappear; }
            set { timeToDissappear = value; }
        }


        public Bubble(Color color,Point position, double size, int time)
        {
            this.color = color;
            this.position = position;
            this.size = size;
            this.timeToDissappear = time;

        }


    }
}