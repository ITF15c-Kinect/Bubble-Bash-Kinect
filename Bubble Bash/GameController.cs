﻿using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using System.Media;

namespace Bubble_Bash
{
    public class GameController
    {
        #region Properties
        public enum State
        {
            MENU, RUNNING, PAUSE, SCOREBOARD
        }

        public State GameState
        {
            get { return state; }
            set
            {

                switch (value)
                {
                    case State.RUNNING:
                        if(state == State.SCOREBOARD)
                        {
                            resetPlayerScore();
                        }
                        watch.Start();
                        break;
                    case State.PAUSE:
                        watch.Stop();
                        break;
                    case State.SCOREBOARD:
                        watch.Reset();
                        break;
                }
                state = value;
            }
        }

        private void resetPlayerScore()
        {
            foreach (Player player in Players)
            {
                player.score = 0;
            }
        }

        private Stopwatch watch = new Stopwatch();

        public int getGameTime()
        {
            return (int) ((gameDuration - watch.ElapsedMilliseconds) / 1000);
        }

        public List<Player> Players = new List<Player>();
        private Player playerOne = null;

        public Player PlayerOne
        {
            get { return playerOne; }
            set { playerOne = value; }
        }
        private Player playerTwo = null;

        public Player PlayerTwo
        {
            get { return playerTwo; }
            set { playerTwo = value; }
        }
        private List<Bubble> bubbles = new List<Bubble>();

        public List<Bubble> Bubbles
        {
            get { return bubbles; }
            set { bubbles = value; }
        }

        public int gameDuration = 45000;

        private bool running;
        private Random rnd;

        private int bubbleMinRadius = 35;
        private int bubbleMaxRadius = 65;

        private int bubbleSpawnRate = 700;

        private int spawnXMin = 300;
        private int spawnXMax = 1620;
        private int spawnYMax = 980;

        private int bubbleMinTime = 2000;
        private int bubbleMaxTime = 4500;

        private DateTime lastBubbleSpawnedAt;
        private MainWindow window;

        private Color bubbleColorRed = Color.FromRgb(255, 0, 0);
        private Color bubbleColorGreen = Color.FromRgb(0, 255, 0);
        private Color bubbleColorBlue = Color.FromRgb(0, 0, 255);

        private SoundPlayer popSound = new SoundPlayer(Properties.Resources.Popsound);

        private State state = State.MENU;
        #endregion

        #region Constructor
        public GameController(MainWindow window)
        {
            this.window = window;
            this.rnd = new Random();
        }
        #endregion

        /// <summary>
        /// thread for the game logic
        /// </summary>
        public void run()
        {
            this.running = true;
            try
            {
                while (running)
                {
                    int startTime = DateTime.Now.Millisecond;
                    checkPlayerState();
                    if (GameState == State.RUNNING)
                    {
                        if(watch.ElapsedMilliseconds > gameDuration)
                        {
                            GameState = State.SCOREBOARD;
                        }
                        despawnBubbles();
                        fadeOut();
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
        /// <summary>
        /// fades out the bubbles 
        /// </summary>
        private void fadeOut()
        {
            lock (Bubbles)
            {
                foreach (var bubble in Bubbles)
                {
                    bubble.BubbleColor = Color.FromArgb((byte)(bubble.BubbleColor.A - 1.5), bubble.BubbleColor.R, bubble.BubbleColor.G, bubble.BubbleColor.B);
                }
            }
        }
        /// <summary>
        /// checks if a body is tracked and if the body is currently an active player otherwise adds them to the game
        /// </summary>
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

            if ((PlayerOne == null || !PlayerOne.body.IsTracked)
                && PlayerTwo != null && PlayerTwo.body.IsTracked)
            {
                PlayerOne = PlayerTwo;
                PlayerTwo = null;
            }

        }
        /// <summary>
        /// removes bubbles if the collision is detected
        /// </summary>
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
        /// <summary>
        /// detects collisions between bubbles and hands and adds score to the player
        /// </summary>
        private bool detectCollision(Player player, Bubble bubble)
        {
            Point leftHand = this.window.getPoint(JointType.HandLeft, player.body);
            Point rightHand = this.window.getPoint(JointType.HandRight, player.body);
            if ((Point.Subtract(leftHand, bubble.BubblePosition).Length < window.HandSize + bubble.BubbleSize && gestureMatches(player.body.HandLeftState, bubble))
                 || (Point.Subtract(rightHand, bubble.BubblePosition).Length < window.HandSize + bubble.BubbleSize && gestureMatches(player.body.HandRightState, bubble)))
            {

                double bubbleRadiusRange = (this.bubbleMaxRadius - this.bubbleMinRadius);
                double bubbleTimeRange = (this.bubbleMaxTime - this.bubbleMinTime);



                player.score += (int)(10 + 45 * (1.0 / bubbleRadiusRange * bubble.BubbleSize - (this.bubbleMinRadius / bubbleRadiusRange)) +
                    45 * (1.0 / bubbleTimeRange * bubble.TimeToDisappear - (this.bubbleMinTime / bubbleTimeRange)));

                Console.WriteLine(player.body.TrackingId + ": Score " + player.score);
                this.popSound.Play();

                return true;
            }
            return false;
        }
        /// <summary>
        /// checks if gesture matches with bubble colors
        /// </summary>
        /// <param name="handState"></param>
        /// <param name="bubble"></param>
        /// <returns></returns>
        private bool gestureMatches(HandState handState, Bubble bubble)
        {
            switch (handState)
            {
                case HandState.Open:
                    return bubble.BubbleColor.G.Equals(bubbleColorGreen.G);
                case HandState.Lasso:
                    return bubble.BubbleColor.B.Equals(bubbleColorBlue.B);
                case HandState.Closed:
                    return bubble.BubbleColor.R.Equals(bubbleColorRed.R);
                default:
                    return false;
            }
        }
        /// <summary>
        /// spwan bubbles with random properties at random locations
        /// </summary>
        private void spawnBubbles()
        {
            try
            {
                DateTime now = DateTime.Now;

                lock (Bubbles)
                {
                    if ((lastBubbleSpawnedAt == null || now - lastBubbleSpawnedAt >= new TimeSpan(0, 0, 0, 0, bubbleSpawnRate)))
                    {
                        Bubble bubble = new Bubble(randomColor(), randomPosition(), rnd.Next(bubbleMinRadius, bubbleMaxRadius + 1), rnd.Next(bubbleMinTime, bubbleMaxTime + 1));
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
        /// <summary>
        /// despawn bubbles from drawing context if their lifespan is over
        /// </summary>
        private void despawnBubbles()
        {
            lock (Bubbles)
            {
                Bubbles.RemoveAll(shouldDespawn);
            }
        }
        /// <summary>
        /// checks if the bubbles should disappear
        /// </summary>
        /// <param name="bubble"></param>
        /// <returns></returns>
        private static bool shouldDespawn(Bubble bubble)
        {
            return (DateTime.Now - bubble.Created > new TimeSpan(0, 0, 0, 0, bubble.TimeToDisappear));
        }
        /// <summary>
        /// gets a random point
        /// </summary>
        /// <returns></returns>
        private Point randomPosition()
        {
            return new Point(rnd.Next(spawnXMin, spawnXMax), rnd.Next(bubbleMaxRadius, spawnYMax));
        }
        /// <summary>
        /// gets a random color
        /// </summary>
        /// <returns></returns>
        private Color randomColor()
        {
            int n = rnd.Next(1, 4);
            switch (n)
            {
                case 1:
                    return bubbleColorRed;
                case 2:
                    return bubbleColorGreen;
                case 3:
                    return bubbleColorBlue;
                default:
                    throw new Exception("Invalid random value: " + n);
            }
        }
        /// <summary>
        /// checks if a body has a player
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
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
        /// <summary>
        /// adds player to the player list
        /// </summary>
        /// <param name="player"></param>
        internal void addPlayer(Player player)
        {
            if (!Players.Contains(player))
            {
                Players.Add(player);
            }
            if (PlayerOne == null)
            {
                PlayerOne = player;
                Console.WriteLine("PlayerOne:" + PlayerOne.body.TrackingId);
            }
            else if (PlayerTwo == null)
            {
                PlayerTwo = player;
                Console.WriteLine("PlayerTwo:" + PlayerTwo.body.TrackingId);
            }
        }
    }
}