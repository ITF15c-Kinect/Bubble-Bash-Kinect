using Microsoft.Kinect;

namespace Bubble_Bash
{
    public class Player
    {
        public Player(Body body)
        {
            this.body = body;

        }
        public int score = 0;
        public Body body;
    }
}