using System;
using System.Windows;
using System.Windows.Media;

namespace Bubble_Bash
{
    public class Bubble
    {  
        #region Properties
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
        private DateTime created;
        public DateTime Created
        {
            get { return created; }
            set { created = value; }
        }
        #endregion

        #region Constructor
        public Bubble(Color color, Point position, double size, int time)
        {
            this.color = color;
            this.position = position;
            this.size = size;
            this.timeToDissappear = time;
            this.Created = DateTime.Now;
        } 
        #endregion
    }
}