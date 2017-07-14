using System;
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

        private double handSize = 50;
        public double HandSize
        {
            get { return handSize; }
            set { handSize = value; }
        }

        private static readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private static readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private static readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        private Body[] bodies = null;

        private int displayWidth;
        private int displayHeight;
        private DrawingGroup drawingGroup;

        private WriteableBitmap colorBitmap = null;

        private const float InferredZPositionClamp = 0.1f;
        private GameController gameController;
        private Thread gameThread;

        private DrawingImage imageSource;
        public ImageSource getImageSource
        {
            get { return this.imageSource; }
        } 
        #endregion

        public MainWindow()
        {
            this.gameController = new GameController(this);
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

            this.imageSource = new DrawingImage(this.drawingGroup);

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
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    dc.DrawImage(this.colorBitmap, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    if (gameController.GameState == GameController.State.RUNNING)
                    {

                    }
                    drawBubbles(dc);
                    drawScore(dc);

                    bool noHandTracked = true;
                    foreach (Body body in this.bodies)
                    {

                        if (body.IsTracked)
                        {
                            if (!this.gameController.hasPlayerFor(body))
                            {
                                this.gameController.addPlayer(new Player(body));
                            }
                            DrawHand(body.HandLeftState, getPoint(JointType.HandLeft, body), dc);
                            DrawHand(body.HandRightState, getPoint(JointType.HandRight, body), dc);
                            noHandTracked = false;

                            if (body.HandLeftState == HandState.Lasso && body.HandRightState == HandState.Lasso)
                            {
                                this.imageMenuScreen.Opacity = 0;
                                this.gameController.GameState = GameController.State.RUNNING;
                            }
                        }
                    }

                    if (noHandTracked && gameController.GameState != GameController.State.MENU)
                    {
                        this.gameController.GameState = GameController.State.PAUSE;
                    }

                    //this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void drawScore(DrawingContext dc)
        {
            var typeface = new Typeface(new FontFamily("Verdana"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
            Brush brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            String playerOneScore = "Player 1\n";
            if (gameController.PlayerOne != null)
            {
                playerOneScore += gameController.PlayerOne.score;
            }
            dc.DrawText(new FormattedText(playerOneScore, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 58, brush), new Point(0, 0));

            String playerTwoScore = "Player 2\n";
            if (gameController.PlayerTwo != null)
            {
                playerTwoScore += gameController.PlayerTwo.score;
            }
            dc.DrawText(new FormattedText(playerTwoScore, CultureInfo.CurrentCulture, FlowDirection.RightToLeft, typeface, 58, brush), new Point(displayWidth, 0));

            if (gameController.GameState == GameController.State.PAUSE )
            {
                dc.DrawText(new FormattedText("Paused", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 58, brush),new Point(displayWidth/2-110,displayHeight/2));
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

        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }      
    }
}
