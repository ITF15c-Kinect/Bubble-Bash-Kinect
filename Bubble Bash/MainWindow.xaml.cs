﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.Threading;

namespace Bubble_Bash
{
    public partial class MainWindow : Window
    {
        #region Properties
        private KinectSensor kinectSensor = null;

        private ColorFrameReader colorFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private CoordinateMapper coordinateMapper = null;

        public double HandSize { get; set; }

        private static readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private static readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private static readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        public Body[] Bodies { get; internal set; }
        #endregion

        private int displayWidth;
        private int displayHeight;
        private DrawingGroup drawingGroup;

        private WriteableBitmap colorBitmap = null;

        private const float InferredZPositionClamp = 0.1f;
        private GameController gameController;
        private Thread gameThread;

        private Typeface typeface;
        private SolidColorBrush textBrush;

        public ImageSource ImageSource { get; internal set; }



        public MainWindow()
        {
            HandSize = 50;
            this.gameController = new GameController(this);
            typeface = new Typeface(new FontFamily("Verdana"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
            textBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            startGameThread();
            InitializeKinect();
            InitializeComponent();
        }

        private void startGameThread()
        {
            this.gameThread = new Thread(this.gameController.run);
            this.gameThread.Start();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.gameThread.Abort();
            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void InitializeKinect()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.displayWidth = colorFrameDescription.Width;
            this.displayHeight = colorFrameDescription.Height;
            this.kinectSensor.Open();

            this.drawingGroup = new DrawingGroup();

            this.ImageSource = new DrawingImage(this.drawingGroup);

            this.DataContext = this;
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.Bodies == null)
                    {
                        this.Bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.Bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    dc.DrawImage(this.colorBitmap, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    drawPlayerHands(gameController.PlayerOne,1, dc);
                    drawPlayerHands(gameController.PlayerTwo,2, dc);

                    bool noHandTracked = true;
                    foreach (Body body in this.Bodies)
                    {

                        if (body.IsTracked)
                        {
                            if (!this.gameController.hasPlayerFor(body))
                            {
                                this.gameController.addPlayer(new Player(body));
                            }
                            //DrawHand(body.HandLeftState, getPoint(JointType.HandLeft, body), dc);
                            //DrawHand(body.HandRightState, getPoint(JointType.HandRight, body), dc);
                            noHandTracked = false;

                            if (body.HandLeftState == HandState.Lasso && body.HandRightState == HandState.Lasso)
                            {
                                this.imageMenuScreen.Opacity = 0;
                                this.gameController.GameState = GameController.State.RUNNING;
                            }
                        }
                    }

                    switch (gameController.GameState)
                    {
                        case GameController.State.PAUSE:
                        case GameController.State.RUNNING:
                            drawBubbles(dc);
                            drawScore(dc);
                            drawTimer(dc);
                            break;
                        case GameController.State.SCOREBOARD:
                            drawScore(dc);
                            drawScoreboard(dc);
                            break;
                    }



                    if (noHandTracked && gameController.GameState == GameController.State.RUNNING)
                    {
                        this.gameController.GameState = GameController.State.PAUSE;
                    }

                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void drawTimer(DrawingContext dc)
        {
            var typeface = new Typeface(new FontFamily("Verdana"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
            Brush brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            String timer = gameController.getGameTime().ToString();
            dc.DrawText(new FormattedText(timer, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 58, brush), new Point(displayWidth / 2, 0));
        }

        private void drawScoreboard(DrawingContext dc)
        {
            if (gameController.PlayerOne != null && gameController.PlayerTwo != null)
            {
                String winnerText = "Winner is Player ";
                if (gameController.PlayerOne.score > gameController.PlayerTwo.score)
                {
                    winnerText += "1\nwith " + gameController.PlayerOne.score + " points!";
                }
                else
                {
                    winnerText += "2\nwith " + gameController.PlayerTwo.score + " points!"; ;
                }
                dc.DrawText(new FormattedText(winnerText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 72, textBrush), new Point(displayWidth / 2 - 350, displayHeight / 2 - 100));
            }
            else
            {
                dc.DrawText(new FormattedText("Your Score:\n" + gameController.PlayerOne.score, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 72, textBrush), new Point(displayWidth / 2 - 410, displayHeight / 2 - 100));
            }
        }

        private void drawPlayerHands(Player player, int playerNumber, DrawingContext dc)
        {
            if (player == null)
                return;
            Body body = player.body;
            DrawHand(body.HandLeftState, getPoint(JointType.HandLeft, body), playerNumber, dc);
            DrawHand(body.HandRightState, getPoint(JointType.HandRight, body), playerNumber, dc);
        }

        private void drawScore(DrawingContext dc)
        {
            String playerOneScore = "Player 1\n";
            if (gameController.PlayerOne != null)
            {
                playerOneScore += gameController.PlayerOne.score;
            }
            dc.DrawText(new FormattedText(playerOneScore, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 58, textBrush), new Point(0, 0));

            String playerTwoScore = "Player 2\n";
            if (gameController.PlayerTwo != null)
            {
                playerTwoScore += gameController.PlayerTwo.score;
            }
            dc.DrawText(new FormattedText(playerTwoScore, CultureInfo.CurrentCulture, FlowDirection.RightToLeft, typeface, 58, textBrush), new Point(displayWidth, 0));


            if (gameController.GameState == GameController.State.PAUSE)
            {
                dc.DrawText(new FormattedText("Paused", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 58, textBrush), new Point(displayWidth / 2 - 110, displayHeight / 2));
            }
        }

        private void drawBubbles(DrawingContext dc)
        {
            var bubbles = this.gameController.Bubbles;
            lock (bubbles)
            {
                foreach (Bubble bubble in bubbles)
                {
                    DrawBubble(bubble, dc);
                }
            }

        }

        private static void DrawBubble(Bubble bubble, DrawingContext drawingContext)
        {
            Color white = new Color();

            white = Color.FromArgb(100, 255, 255, 255);

            //Brush brush = new LinearGradientBrush(white, bubble.BubbleColor, 45);
            Brush brush = new RadialGradientBrush(white, bubble.BubbleColor);
            //Brush brush = new SolidColorBrush(Color.FromArgb(150, bubble.BubbleColor.R, bubble.BubbleColor.G, bubble.BubbleColor.B));

            Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), 5);
            drawingContext.DrawEllipse(brush, pen, new Point(bubble.BubblePosition.X, bubble.BubblePosition.Y), bubble.BubbleSize, bubble.BubbleSize);

            //Brush whiteBrush = new SolidColorBrush(Color.FromArgb(100,255,255,255));
            //Point reflectionPoint = new Point(bubble.BubblePosition.X - bubble.BubbleSize / 2.5, bubble.BubblePosition.Y - bubble.BubbleSize / 3);
            //drawingContext.DrawEllipse(brush2, null, reflectionPoint, bubble.BubbleSize/3, bubble.BubbleSize / 3);
        }

        public Point getPoint(JointType type, Body body)
        {
            CameraSpacePoint jointPosition = body.Joints[type].Position;
            if (jointPosition.Z < 0)
            {
                jointPosition.Z = InferredZPositionClamp;
            }
            ColorSpacePoint jointColorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(jointPosition);
            return new Point(jointColorSpacePoint.X, jointColorSpacePoint.Y);
        }

        private void DrawHand(HandState handState, Point handPosition, int playerNumber, DrawingContext drawingContext)
        {
            Point p = handPosition;
            p.X -= 10;
            p.Y -= 20;
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(handClosedBrush, null, handPosition, HandSize, HandSize);
                    drawingContext.DrawText(new FormattedText(playerNumber.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 30, textBrush), p);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(handOpenBrush, null, handPosition, HandSize, HandSize);
                    drawingContext.DrawText(new FormattedText(playerNumber.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 30, textBrush), p);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(handLassoBrush, null, handPosition, HandSize, HandSize);
                    drawingContext.DrawText(new FormattedText(playerNumber.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 30, textBrush), p);
                    break;
            }
        }
    }
}
