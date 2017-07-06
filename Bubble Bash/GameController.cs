using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;

namespace Bubble_Bash
{
    public class GameController
    {
        public enum State
        {
            TUTORIAL, RUNNING, PAUSE
        }

        public List<Player> Players = new List<Player>();
        private Player playerOne = null;
        private Player playerTwo = null;
        private List<Bubble> bubbles = new List<Bubble>();
        private bool running;
        private Random rnd;

        private int bubbleMaxRadius = 65;
        private int bubbleMinRadius = 35;

        private int bubbleSpawnRate = 1500;
        private int maxBubbles = 100;

        private int spawnXMin = 300;
        private int spawnXMax = 1620;
        private int spawnYMax = 980;

        public GameController(MainWindow window)
        {
            this.window = window;
            this.rnd = new Random();
        }

        private State state = State.PAUSE;
        public void run()
        {
            initialize();
            this.running = true;
            try
            {
                while (running)
                {
                    int startTime = DateTime.Now.Millisecond;
                    checkPlayerState();
                    if (GameState == State.RUNNING)
                    {
                        spawnBubbles();
                        detectCollisions();
                    }
                    int finishTime = DateTime.Now.Millisecond;
                    System.Threading.Thread.Sleep(Math.Max(33 - (finishTime - startTime), 0));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void checkPlayerState()
        {
            if (PlayerOne != null && !PlayerOne.body.IsTracked)
            {
                foreach (Player player in Players)
                {
                    if (!player.Equals(PlayerTwo) && player.body.IsTracked)
                    {
                        PlayerOne = player;
                        Console.WriteLine("New Player 1: " + player.body.TrackingId);
                        break;
                    }
                }
            }
            if (PlayerTwo != null && !PlayerTwo.body.IsTracked)
            {
                foreach (Player player in Players)
                {
                    if (!player.Equals(PlayerOne) && player.body.IsTracked)
                    {
                        PlayerTwo = player;
                        Console.WriteLine("New Player 2: " + player.body.TrackingId);
                        break;
                    }
                }
            }
        }

        private void initialize()
        {
        }

        private void detectCollisions()
        {
            lock (Bubbles)
            {
                List<Bubble> remove = new List<Bubble>();
                foreach (Bubble bubble in Bubbles)
                {
                    if (PlayerOne != null)
                    {
                        if (detectCollision(PlayerOne, bubble))
                        {
                            remove.Add(bubble);
                        }
                    }

                    if (PlayerTwo != null)
                    {
                        if (detectCollision(PlayerTwo, bubble))
                        {
                            remove.Add(bubble);
                        }
                    }
                }
                foreach (Bubble bubble in remove)
                {
                    Bubbles.Remove(bubble);
                }
            }
        }

        private bool detectCollision(Player player, Bubble bubble)
        {
            Point leftHand = this.window.getPoint(JointType.HandLeft, player.body);
            Point rightHand = this.window.getPoint(JointType.HandRight, player.body);
            if ((Point.Subtract(leftHand, bubble.BubblePosition).Length < window.HandSize + bubble.BubbleSize && gestureMatches(player.body.HandLeftState, bubble))
                 || (Point.Subtract(rightHand, bubble.BubblePosition).Length < window.HandSize + bubble.BubbleSize && gestureMatches(player.body.HandRightState, bubble)))
            {
                player.score += 50;
                Console.WriteLine(player.body.TrackingId + ": Score " + player.score);
                return true;
            }
            return false;
        }

        private bool gestureMatches(HandState handState, Bubble bubble)
        {
            switch (handState)
            {
                case HandState.Open:
                    return bubble.BubbleColor.Equals(BubbleColorGreen);
                case HandState.Lasso:
                    return bubble.BubbleColor.Equals(BubbleColorBlue);
                case HandState.Closed:
                    return bubble.BubbleColor.Equals(BubbleColorRed);
                default:
                    return false;
            }
        }

        private DateTime lastBubbleSpawnedAt;
        private MainWindow window;

        public List<Bubble> Bubbles
        {
            get
            {
                return bubbles;
            }

            set
            {
                bubbles = value;
            }
        }

        public State GameState
        {
            get
            {
                return state;
            }

            set
            {
                state = value;
            }
        }

        public Player PlayerOne
        {
            get
            {
                return playerOne;
            }

            set
            {
                playerOne = value;
            }
        }

        public Player PlayerTwo
        {
            get
            {
                return playerTwo;
            }

            set
            {
                playerTwo = value;
            }
        }

        private void spawnBubbles()
        {
            try
            {
                DateTime now = DateTime.Now;

                lock (Bubbles)
                {
                    if (Bubbles.Count < maxBubbles && (lastBubbleSpawnedAt == null || now - lastBubbleSpawnedAt >= new TimeSpan(0, 0, 0, 0, bubbleSpawnRate)))
                    {
                        Bubble bubble = new Bubble(randomColor(), randomPosition(), rnd.Next(bubbleMinRadius, bubbleMaxRadius), 5000);
                        this.Bubbles.Add(bubble);

                        lastBubbleSpawnedAt = now;
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private Point randomPosition()
        {
            return new Point(rnd.Next(spawnXMin, spawnXMax), rnd.Next(bubbleMaxRadius, spawnYMax));
        }

        private Color BubbleColorRed = Color.FromRgb(255, 0, 0);
        private Color BubbleColorGreen = Color.FromRgb(0, 255, 0);
        private Color BubbleColorBlue = Color.FromRgb(0, 0, 255);

        private Color randomColor()
        {
            int n = rnd.Next(1, 4);
            switch (n)
            {
                case 1:
                    return BubbleColorRed;
                case 2:
                    return BubbleColorGreen;
                case 3:
                    return BubbleColorBlue;
                default:
                    throw new Exception("Invalid random value: " + n);
            }

        }

        internal bool hasPlayerFor(Body body)
        {
            foreach (Player player in Players)
            {
                if (player.body == body)
                {
                    return true;
                }
            }
            return false;
        }

        internal void addPlayer(Player player)
        {
            if (!Players.Contains(player))
            {
                Players.Add(player);
            }
            if (PlayerOne == null)
            {
                PlayerOne = player;
            }
            else if (PlayerTwo == null)
            {
                PlayerTwo = player;
            }
        }
    }
}