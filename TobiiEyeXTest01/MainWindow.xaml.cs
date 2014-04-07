using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using InteractorId = System.String;

namespace TobiiEyeXTest01
{
    public partial class MainWindow : Window, IDisposable
    {
        private const string InteractorId = "WPF_Test";

        //private InteractionSystem _system;
        private InteractionContext _context;
        private InteractionSnapshot _globalInteractorSnapshot;
        InteractionSystem system = InteractionSystem.Initialize(LogTarget.Trace);
        
        //GazeAwareButton
         Point gaze;
         bool paint = false;
         //bool menuActive = true;
         Button activeButton;
         Dictionary<InteractorId, Button> gazeAwareButtons;
            

        
        //For Picture
        static readonly int pictureWidth = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
        static readonly int pictureHeight = (int)(System.Windows.SystemParameters.PrimaryScreenHeight * 0.8); //TODO CHANGE 0.8 TO Constant 
        bool firstTime = true;
        RenderTargetBitmap bmp = new RenderTargetBitmap(pictureWidth, pictureHeight, 96, 96, PixelFormats.Pbgra32);
        String[] iconNames = new String[7];
        Random rnd = new Random();
        SolidColorBrush mySolidColorBrush = new SolidColorBrush();

        public MainWindow()
        {
            String path = Directory.GetCurrentDirectory() + "\\data\\blue.png";
            Console.WriteLine(path);
            
            InitializeComponent();

            //Represent the OnLoad in old version
            // create a context, register event handlers, and enable the connection to the engine.
            _context = new InteractionContext(false);
            InitializeGlobalInteractorSnapshot();
            _context.ConnectionStateChanged += (object s, ConnectionStateChangedEventArgs ce) =>
                {
                    if (ce.State == ConnectionState.Connected)
                    {
                        _globalInteractorSnapshot.Commit((InteractionSnapshotResult isr) => { });
                    }
                };
            _context.RegisterQueryHandlerForCurrentProcess(handleInteractionQuery);
            _context.RegisterEventHandler(handleInteractionEvent);
            _context.EnableConnection();
            
            gazeAwareButtons = new Dictionary<InteractorId, Button>();
            
            initalizeGazeAwareButtons();
        }

        public void initalizeGazeAwareButtons()
        {
            gazeAwareButtons.Add(TestButton.Name, TestButton);
        }

        public void OnViewClick(object sender, RoutedEventArgs e)
        {
            DrawHundredElipses();
        }
            void DrawHundredElipses(){

            int xcord = rnd.Next(pictureWidth);
            int ycord = rnd.Next(pictureHeight);
            int radius = 100;
            if (firstTime)
            {

                Point test1 = new Point(0, 0);
                Point test2 = new Point(pictureWidth, 0);
                Point test3 = new Point(0, pictureHeight);
                Point test4 = new Point(pictureWidth, pictureHeight);
                mySolidColorBrush.Color = Color.FromArgb(255, 255, 100, 100);
                drawElipseOnCanvas(test1, 100);
                drawElipseOnCanvas(test2, 100);

                drawElipseOnCanvas(test3, 100);

                drawElipseOnCanvas(test4, 100);

                //mySolidColorBrush.Color = Color.FromArgb(255, 100, 100, 100);

            }
            else
            {

                for (int i = 0; i <= 100; i++)
                {
                    mySolidColorBrush = new SolidColorBrush();
                    byte red = (byte)rnd.Next(255);
                    byte yellow = (byte)rnd.Next(255);
                    //mySolidColorBrush.Color = Color.FromArgb(255, 255, 100, 100);
                    mySolidColorBrush.Color = Color.FromArgb(255, red, yellow, 100);
                    radius = rnd.Next(50);

                    xcord = rnd.Next(pictureWidth);
                    ycord = rnd.Next(pictureHeight);

                    Point p = new Point(xcord, ycord);
                    drawElipseOnCanvas(p, radius);

                }

            }
            firstTime = false;

        }

        public void drawElipseOnCanvas(Point p, int radius)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawEllipse(mySolidColorBrush, null, p, radius, radius);
            drawingContext.Close();
            bmp.Render(drawingVisual);
            currentImage.Source = bmp;


        } 
    

        //Checked
        private void InitializeGlobalInteractorSnapshot()
        {
            _globalInteractorSnapshot = _context.CreateSnapshot();
            _globalInteractorSnapshot.CreateBounds(InteractionBoundsType.None);
            _globalInteractorSnapshot.AddWindowId(Literals.GlobalInteractorWindowId);

            var interactor = _globalInteractorSnapshot.CreateInteractor(InteractorId, Literals.RootId, Literals.GlobalInteractorWindowId);
            interactor.CreateBounds(InteractionBoundsType.None);

            var behavior = interactor.CreateBehavior(InteractionBehaviorType.GazePointData);
            var behaviorParams = new GazePointDataParams() { GazePointDataMode = GazePointDataMode.LightlyFiltered };
            behavior.SetGazePointDataOptions(ref behaviorParams);

        }

        void handleInteractionQuery(InteractionQuery q)
        {
            double x, y, w, h;
            if (q.Bounds.TryGetRectangularData(out x, out y, out w, out h))
            {
                System.Windows.Rect queryBounds = new System.Windows.Rect((int)x, (int)y, (int)w, (int)h);
                this.Dispatcher.Invoke(() => ActionWhenInteractionQuerry(queryBounds,q));
            }
        }
        
            
        private void ActionWhenInteractionQuerry(System.Windows.Rect queryBounds, InteractionQuery q){
                    // Prepare a new snapshot.
                    InteractionSnapshot s = _context.CreateSnapshotWithQueryBounds(q);
                    
                    //TODO Check this is correct
                    IntPtr windowHandle = new WindowInteropHelper(PaintingWindow).Handle;
                    s.AddWindowId(windowHandle.ToString());

                    // Determine if the user is looking at the menu or the canvas
                    //menuActive = false;
                    //TODO Check what happend if panel not initialized or visible
                    Point menuCorner = menuPanel.PointToScreen(new Point(0,0));
                    
                    System.Windows.Rect menuBounds = new System.Windows.Rect((int) menuCorner.X,(int) menuCorner.Y,(int) menuPanel.ActualWidth, (int) menuPanel.ActualHeight); 
                    
                    if (menuBounds.IntersectsWith(queryBounds))
                    {
                      //  menuActive = true;

                        // Create a new gaze aware interactor for buttons within the query bounds.
                        foreach (var e in gazeAwareButtons)
                        {
                            Button b = e.Value;
                            InteractorId id = e.Key;
                            Point buttonCorner = b.PointToScreen(new Point(0,0));

                            System.Windows.Rect buttonBounds = new System.Windows.Rect(buttonCorner.X, buttonCorner.Y,b.ActualWidth,b.ActualHeight);

                            if (buttonBounds.IntersectsWith(queryBounds))
                            {
                                IntPtr Handle = new WindowInteropHelper(PaintingWindow).Handle;
                                Interactor i = s.CreateInteractor(id, Literals.RootId, Handle.ToString());
                                i.CreateBounds(InteractionBoundsType.Rectangular).SetRectangularData(
                                    buttonBounds.Left,
                                    buttonBounds.Top,
                                    buttonBounds.Width,
                                    buttonBounds.Height
                                );
                                i.CreateBehavior(InteractionBehaviorType.GazeAware);
                            }
                        }
                    }

                    // Send the snapshot to the eye tracking server.
                    s.Commit((InteractionSnapshotResult isr) => { });
                }

        
            
        // Track gaze point if it's far away enough from the previous point, and add it
        // to the model if the user wants to.
        void trackGaze(Point p, bool keep = true, int keyhole = 25)
        {
            var distance = Math.Sqrt(Math.Pow(gaze.X - p.X, 2) + Math.Pow(gaze.Y - p.Y, 2));
            if (distance < keyhole) return;
            gaze = p;

            //TODO Change here after test

            //if (keep) model.Add(gaze, true); //TODO Add alwaysAdd argument, or remove it completely from the function declaration.      
        }
        

        // Handle events from the EyeX engine.
        void handleInteractionEvent(InteractionEvent e)
        {
            foreach (var behavior in e.Behaviors)
            {
                if (behavior.BehaviorType == InteractionBehaviorType.GazePointData)
                {
                    GazePointDataEventParams r;
                    if (behavior.TryGetGazePointDataEventParams(out r))
                        trackGaze(new Point(r.X, r.Y), paint, 200); //TODO Set keyhole size dynamically based on how bad the calibration is.
                    this.Dispatcher.Invoke(()=> drawElipseOnCanvas(gaze, 100));
                            
                }
                else if (behavior.BehaviorType == InteractionBehaviorType.GazeAware)
                {
                    GazeAwareEventParams r;
                    if (behavior.TryGetGazeAwareEventParams(out r))
                    {
                        bool hasGaze = r.HasGaze != EyeXBoolean.False;
                        Action a = () =>
                        {
                            //activeButton = gazeAwareButtons[e.InteractorId];
                            //activeButton.Focus();
                            DrawHundredElipses();
                        };

                        this.Dispatcher.Invoke(() => a);
                        //if (IsHandleCreated && hasGaze) BeginInvoke(a);
                    }
                }
            }
        }



        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }

            system.Dispose();
        }
    }
}
