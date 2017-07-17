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

        private int timeToDisappear;
        public int TimeToDisappear
        {
            get { return timeToDisappear; }
            set { timeToDisappear = value; }
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
            this.timeToDisappear = time;
            this.Created = DateTime.Now;
        } 
        #endregion
    }
}