using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Concurrent;
using GLDrawerCLR;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Drawing;

namespace GLDrawer
{
    /// <summary>
    /// Canvas element and window that can render shapes within it
    /// </summary>
    public partial class GLCanvas : IDisposable
    {
        private bool usePackedShaders = true; //set this as true to compile the last packed version of the shaders into the executable/DLL (see shaders.h for more info)

        private float iTime = 0, iDeltaTime = 0, lastTime = 0;
        /// <summary>time in seconds since canvas creation</summary>
        public float Time { get => iTime; }
        /// <summary>time in seconds of the last frame time</summary>
        public float DeltaTime { get => iDeltaTime; }
        /// <summary>text to display on the window title</summary>
        public string WindowTitle { get => GLWrapper.title; set => GLWrapper.title = value; }
        /// <summary>display debug info next to the title</summary>
        public bool ExtraInfo { set => GLWrapper.titleDetails = value; }
        /// <summary>center coordinate of the canvas in pixels (half the window size)</summary>
        public vec2 Center { get => new vec2(Width / 2, Height / 2); }
        /// <summary>globally offsets the coordinate system</summary>
        public vec2 CameraPosition { get => new vec2(GLWrapper.camera.x, GLWrapper.camera.y); set => GLWrapper.camera = new unmanaged_vec2(value.x, value.y); }
        private float iCameraZoom = 1; //zoom is a vec2, so if a user wants to read from their changes, they are stored here
        /// <summary>Uniformly sets the camera scale</summary>
        public float CameraZoom
        {
            get => iCameraZoom;
            set
            {
                iCameraZoom = value;
                GLWrapper.zoom = new unmanaged_vec2(value, value);
            }
        }
        /// <summary>scale, stretche, or invert the coordinate system</summary>
        public vec2 CameraScale { get => new vec2(GLWrapper.zoom.x, GLWrapper.zoom.y); set => GLWrapper.zoom = new unmanaged_vec2(value.x, value.y); }
        public int GameObjectCount => GORefs.Count;
        /// <summary>number of shapes being drawn on the canvas</summary>
        public int ShapeCount { get => GLWrapper.shapeCount; }
        public bool VSync { get; private set; } //these four are set at initialization
        public bool DebugMode { get; private set; }
        public bool Borderless { get; private set; }
        public bool AutoRender { get; private set; }

        private Color iBackColor; //the back color is never changed internally and needs to be saved before initialization
        public Color BackColor
        {
            get => iBackColor;
            set
            {
                iBackColor = value;
                if(GLWrapper != null)
                {
                    GLWrapper.backColor = value;
                    GLWrapper.clearBB();
                }
            }
        }

        //getting the window size is not thread safe due to pointers, furthermore, changing window size at runtime, (especially from c#)
        //can cause severe memory issues. especially with the internal setpixel texture buffer. The main thread needs to be frozen for one frame update to handle the change size
        private int iWidth, iHeight;
        /// <summary>width of the canvas/window</summary>
        public int Width {
            get => iWidth;
            set
            {
                GLWrapper.width = value;
                frozen = true;
                while (frozen) { }
            }
        }
        /// <summary>height of the canvas/window</summary>
        public int Height
        {
            get => iHeight;
            set
            {
                GLWrapper.height = value;
                frozen = true;
                while (frozen) { }
            }
        }

        /// <summary>direction and amount of gravity for physics/summary>
        public vec2 Gravity
        {
            get
            {
                unmanaged_vec2 v = GLWrapper.getGravity();
                return new vec2(v.x, v.y);
            }
            set => GLWrapper.setGravity(value.x, value.y);
        }

        /// <summary>
        /// an event which occurs once per frame, before the standard update
        /// </summary>
        public event Action EarlyUpdate = delegate { };
        /// <summary>
        /// an event which occurs once per frame 
        /// </summary>
        public event Action Update = delegate { };
        /// <summary>
        /// an event which occurs once per frame, after the standard update and after the scene has been rendered
        /// </summary>
        public event Action LateUpdate = delegate { };

        public delegate void GLResizeEvent(int Width, int Height, GLCanvas Canvas);
        public delegate void GLCloseEvent(GLCanvas Canvas);
        public event GLCloseEvent OnClose = delegate { };

        internal unmanaged_Canvas GLWrapper;
        private Form iform;
        private Panel ipanel;
        private bool embedded = false;
        internal bool initialized = false;
        private bool initializedWithLegacyCoords = false; //used by addCentered Text to ensure expected behaviour
        private bool disposed = false;
        private bool frozen = false; //used to emergency freeze the main thread in case of runtime memory alocation such as a window resize
        private bool busy = false; //used to lock the main thread for all canvas' if they're being pushed too hard (like updating 3 windows faster than they can be rendered)
        private bool renderNextFrame = true; //tracks manual rendering
        private static bool singleContext = true; //Skips switching Opengl context if there is only one canvas which can half render times. 
        private List<Shape> shapeRefs = new List<Shape>();
        private List<GameObject> GORefs = new List<GameObject>();
        private List<DelayedCall> delayedCalls = new List<DelayedCall>(); //primary used for games
        internal List<Action> disposeBuffer = new List<Action>();
        private ConcurrentQueue<unmanaged_GO> shapeAddBuffer = new ConcurrentQueue<unmanaged_GO>();
        private ConcurrentQueue<unmanaged_GO> shapeRemoveBuffer = new ConcurrentQueue<unmanaged_GO>();
        private ConcurrentQueue<Pixel> setPixelBuffer = new ConcurrentQueue<Pixel>();

        private static List<GLCanvas> activeCanvases = new List<GLCanvas>();
        private static Thread loopthread;
        private static Thread mainThread;
        private static bool threadsInitialized = false;
        private object transferLock = new object();

        private void StartCanvas(GLCanvas canvas)
        {
            activeCanvases.Add(canvas);
            if (!threadsInitialized)
            {
                mainThread = Thread.CurrentThread;
                threadsInitialized = true;
                loopthread = new Thread(new ThreadStart(ThreadLoop));

                loopthread.Start();
            }
            else
                singleContext = false;

            //it's a bad idea to continue before the canvas is initialized
            while (GLWrapper == null || !GLWrapper.initialized)
                Thread.Sleep(0);
        }

        private static void ThreadLoop()
        {
            while (true)
            {
                //if the end of the main program is reached, all the canvas windows should close
                    if (!mainThread.IsAlive)
                    for (int i = 0; i < activeCanvases.Count; i++)
                        activeCanvases[i].GLWrapper.close();

                //when all canvasas are closed, the thread is aborted
                if (activeCanvases.Count == 0)
                {
                    threadsInitialized = false;
                    Thread.CurrentThread.Abort();
                }

                //canvasas that have closed are simply removed from the update list
                for (int i = 0; i < activeCanvases.Count; i++)
                {
                    GLCanvas can = activeCanvases[i];
                    if (!can.initialized)
                    {
                        if (can.embedded)
                        {
                            can.GLWrapper.createCanvas(can.iWidth, can.iHeight, true, can.BackColor, can.VSync, can.DebugMode);
                            SetParent(can.GLWrapper.getNativeHWND(), can.panelHandle);
                        }
                        else
                            can.GLWrapper.createCanvas(can.iWidth, can.iHeight, can.Borderless, can.BackColor, can.VSync, can.DebugMode);
                        can.Initialize();
                    }         
                    if (activeCanvases[i].GLWrapper.disposed)
                    {
                        activeCanvases[i].OnClose.Invoke(activeCanvases[i]);
                        activeCanvases.RemoveAt(i);
                        continue;
                    }
                    //can't can't try to render before buffers have been swapped from last frame
                    while (can.busy)
                        Thread.Sleep(0);
                    can.MainLoop();
                }
            }
        }

        //handle for when a console app is closed with the exit button
        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        static GLCanvas()
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
            //    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                Debug.WriteLine("Manual console close detected. Starting emergency cleanup...");
                for (int i = 0; i < activeCanvases.Count; i++)
                {
                    activeCanvases[i].GLWrapper.close();
                    activeCanvases[i].MainLoop();
                }       
            }   
            return false;
        }

        //private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        //{
        //}

        /// <summary>
        /// Creates a new window with a GLCanvas
        /// </summary>
        /// <param name="width">Width of the window</param>
        /// <param name="height">Height of the window</param>
        /// <param name="windowTitle">Name of the window title</param>
        /// <param name="BackColor">Background color of the canvas</param>
        /// <param name="LegacyCoordinates">Sets the camera scale and position to emulate the GDIDraw system</param>
        /// <param name="TitleDetails">Displays render time, FPS, and shape count in the title</param>
        /// <param name="VSync">Limits the framerate to 60fps and waits for vertical screen synchronization. Set to false for uncapped framerate</param>
        /// <param name="autoRender">wether to automatically render objects on the canvas each frame</param>
        /// <param name="debugMode">Display rendering information on top of the canvas</param>
        /// <param name="borderless">Wether or not the window is borderless</param>
        public GLCanvas(int width = 800, int height = 600, string windowTitle = "Canvas Window", Color? BackColor = null, bool LegacyCoordinates = true, bool TitleDetails = true, bool VSync = true, bool autoRender = true, bool debugMode = false, bool borderless = false)
        {
            GLWrapper = new unmanaged_Canvas(usePackedShaders)
            {
                title = windowTitle,
                titleDetails = TitleDetails
            };
            iWidth = width;
            iHeight = height;
            this.VSync = activeCanvases.Count > 0 ? false : VSync; //can't have more than one context with vsync, or it will run at 30fps
            Borderless = borderless;
            DebugMode = debugMode;
            iBackColor = BackColor == null ? Color.Black : (Color)BackColor;
            AutoRender = autoRender;
            renderNextFrame = autoRender;
            initializedWithLegacyCoords = LegacyCoordinates;

            if (LegacyCoordinates)
                Invoke(SetInvertedCoordinates);

            StartCanvas(this);
        }

        //allows the native HWND process from a GLFW window to be embedded into a windows forms parnel
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        IntPtr panelHandle;

        /// <summary>
        /// Creates a canvas which is embedded into a windows forms panal.
        /// VSync is forced on as windows likes to run forms in no more than 60 FPS
        /// </summary>
        /// <param name="form">The Windows Form to embed the canvas window</param>
        /// <param name="panel">the panel to replace with the canvas</param>
        /// <param name="BackColor">Background color of the canvas</param>
        /// <param name="autoRender">wether to automatically render objects on the canvas each frame </param>
        /// <param name="debugMode">Display rendering information on top of the canvas</param>
        public GLCanvas(Form form, Panel panel, Color? BackColor = null, bool autoRender = true, bool debugMode = false)
        {
            GLWrapper = new unmanaged_Canvas(usePackedShaders);
            iHeight = panel.Height;
            iWidth = panel.Width;
            form.FormClosed += delegate { Close(); }; //close the canvas when the parent form is closed
            panelHandle = panel.Handle;
            this.VSync = activeCanvases.Count > 0 ? false : true; //can't have more than one context with vsync, or it will run at 30fps
            Borderless = true;
            DebugMode = debugMode;
            iform = form;
            ipanel = panel;
            iBackColor = BackColor == null ? Color.Black : (Color)BackColor;
            embedded = true;
            AutoRender = autoRender;
            renderNextFrame = autoRender;

            StartCanvas(this);
        }
        //C++ backend needs to know where to trigger input events
        private void Initialize()
        {
            GLWrapper.setMouseCallback(MouseCallback);
            GLWrapper.setKeyCallback(KeyCallback);
            GLWrapper.setMouseMoveCallback(MouseMoveCallback);
            initialized = true;
        }

        private bool firstLoop = true;
        private void MainLoop()
        {
            busy = true;

            //let OpenGL warm up a bit before calling time sensitive delegates
            if (firstLoop)      
                firstLoop = false;        
            else
            {
                iTime = GLWrapper.ellapsedTime;
                iDeltaTime = iTime - lastTime;
                lastTime = iTime;

                EarlyUpdate.Invoke();
                Update.Invoke();
                LateUpdate.Invoke();

                //user invoke list called here as well as thread spesific function call
                for (int i = 0; i < delayedCalls.Count; i++)
                    delayedCalls[i].timeLeft -= DeltaTime;
                for (int i = 0; i < delayedCalls.Count; i++)
                {
                    if (delayedCalls[i].timeLeft <= 0 && delayedCalls[i].func != null)
                    {
                        delayedCalls[i].func.Invoke();
                        if (delayedCalls[i].repeating)
                            delayedCalls[i].timeLeft = delayedCalls[i].initialTime;
                    }
                }
                delayedCalls.RemoveAll(o => o.timeLeft <= 0 || o.func == null);
            }
            unmanaged_GO bufferGO;
            while (shapeAddBuffer.TryDequeue(out bufferGO))
                GLWrapper.addGO(bufferGO);

            while (shapeRemoveBuffer.TryDequeue(out bufferGO))
                GLWrapper.removeGO(bufferGO);

            Pixel p;
            while (setPixelBuffer.TryDequeue(out p))
            {
                GLWrapper.setBBpixel(p.x, p.y, p.color.R, p.color.G, p.color.B, p.color.A);
            }            

            MouseScrollDirection = 0;

            //needs to be very spesific due to threads
            if (!AutoRender)
            {
                if (renderNextFrame)
                {
                    GLWrapper.mainloop(true, !singleContext);
                    renderNextFrame = false;
                }
                else
                    GLWrapper.mainloop(false, !singleContext);
            }
            else
                GLWrapper.mainloop(true, !singleContext);

            disposeBuffer.ForEach(a => a.Invoke());
            disposeBuffer.Clear();

            iWidth = GLWrapper.width;
            iHeight = GLWrapper.height;
            frozen = false;

            if (GLWrapper.reSize)
            {
                GLWrapper.reSize = false;
                CanvasResized.Invoke(iWidth, iHeight, this);
            }
            busy = false;
        }

        //shortcut for converting null colors to transparent colors for default parameters
        private Color CheckNullC(Color ? c)
        {
            return c == null ? Color.Invisible : (Color)c;
        }

        private void AddToBuffer(Shape s)
        {
            //GLWrapper.addGO(s.internalGO);
            shapeAddBuffer.Enqueue(s.internalGO);
            shapeRefs.Add(s);
        }
        private void AddToRemoveBuffer(Shape s)
        {
            //GLWrapper.addGO(s.internalGO);
                        shapeRefs.Remove(s);
            shapeRemoveBuffer.Enqueue(s.internalGO);
        }

        private int lastDrawIndex = 1;
        private int nextDrawIndex()
        {
            lastDrawIndex--;
            return (lastDrawIndex);
        }

        /// <summary>
        /// Add a Rectangle to the sanvas
        /// </summary>
        /// <param name="XStart">Bounding box Left/X start Coordinate</param>
        /// <param name="YStart">Bounding box Top/Y start Coordinate</param>
        /// <param name="Width">Bounding box Width</param>
        /// <param name="Height">Bounding box Height</param>
        /// <param name="FillColor">Fill Color</param>
        /// <param name="BorderThickness">Border Thickness</param>
        /// <param name="BorderColor">Border Color</param>
        /// <param name="Angle">Rotation around the center point in radians</param> 
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Polygon AddRectangle(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Polygon r = new Polygon(new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), Angle, 4, FillColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(r);
            return r;
        }
        /// <summary>
        /// Add a centered Rectangle to the canvasa
        /// </summary>
        /// <param name="Xpos">X Coordinate of Rectangle center point</param>
        /// <param name="Ypos">Y Coordinate of Rectangle center point</param>
        /// <param name="Width">The width of the Rectangle</param>
        /// <param name="Height">The height of the Rectangle</param>
        /// <param name="FillColor">The Rectangle fill color</param>
        /// <param name="BorderThickness">Thickness of the outside border</param>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="Angle">Rotation around the center point in radians</param> 
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Polygon AddCenteredRectangle(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Polygon r = new Polygon(new vec2(Xpos, Ypos), new vec2(Width, Height), Angle, 4, FillColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(r);
            return r;
        }
        /// <summary>
        /// Add an Ellipse to the sanvas
        /// Add an Ellipse to the sanvas
        /// </summary>
        /// <param name="XStart">Bounding box Left/X start Coordinate</param>
        /// <param name="YStart">Bounding box Top/Y start Coordinate</param>
        /// <param name="Width">Bounding box Width</param>
        /// <param name="Height">Bounding box Height</param>
        /// <param name="FillColor">Fill Color</param>
        /// <param name="BorderThickness">Border Thickness</param>
        /// <param name="BorderColor">Border Color</param>
        /// <param name="Angle">Rotation around the center point in radians</param> 
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Polygon AddEllipse(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Polygon e = new Polygon(new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), Angle, 1, FillColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(e);
            return e;
        }
        /// <summary>
        /// Add a centered Ellipse to the canvasa
        /// </summary>
        /// <param name="Xpos">X Coordinate of Ellipse center point</param>
        /// <param name="Ypos">Y Coordinate of Ellipse center point</param>
        /// <param name="Width">The width of the Ellipse</param>
        /// <param name="Height">The height of the Ellipse</param>
        /// <param name="FillColor">The Ellipse fill color</param>
        /// <param name="BorderThickness">Thickness of the outside border</param>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="Angle">Rotation around the center point in radians</param> 
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Polygon AddCenteredEllipse(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Polygon e = new Polygon(new vec2(Xpos, Ypos), new vec2(Width, Height), Angle, 1, FillColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(e);
            return e;
        }
        /// <summary>
        /// Add a Line to the canvas where start and end points are defined
        /// </summary>
        /// <param name="XStart">X Coordinate of Start Point</param>
        /// <param name="YStart">Y Coordinate of Start Point</param>
        /// <param name="XEnd">X Coordinate of End Point</param>
        /// <param name="YEnd">Y Coordinate of End Point</param>
        /// <param name="Thickness">Line Thickness</param>
        /// <param name="LineColor">Line Color</param> 
        /// <param name="BorderThickness">Thickness of the outside border</param>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Line AddLine(float XStart, float YStart, float XEnd, float YEnd, float Thickness, Color? LineColor = null, float BorderThickness = 0, Color? BorderColor = null, float RotationSpeed = 0)
        {
            Line l = new Line(new vec2(XStart, YStart), new vec2(XEnd, YEnd), Thickness, LineColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(l);
            return l;
        }
        /// <summary>
        /// Add a line segment to the canvas at a known start point using a length and rotation angle
        /// </summary>
        /// <param name="StartPos">Start Point of the Line</param>
        /// <param name="Length">Length of Line segment</param>
        /// <param name="Angle">Rotation around start in Radians ( 0 is Up )</param>
        /// <param name="Thickness">Width of the line</param>
        /// <param name="LineColor">Line Color</param>
        /// <param name="BorderThickness">Thickness of the outside border</param>>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Line AddLine(vec2 StartPos, float Length, float Angle, float Thickness, Color? LineColor = null, float BorderThickness = 0, Color? BorderColor = null, float RotationSpeed = 0)
        {
            Line l = new Line(StartPos, Length, Thickness, Angle, LineColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(l);
            return l;
        }
        /// <summary>
        /// Add a centered polygon to the canvas
        /// </summary>
        /// <param name="Xpos">X Coordinate of Polygon center point</param>
        /// <param name="Ypos">Y Coordinate of Polygon center point</param>>
        /// <param name="Width">Width of the Polygon</param>
        /// <param name="Height">Height of the Polygon</param>
        /// <param name="SideCount">number of sides of the Polygon</param>
        /// <param name="FillColor">Inside color of the Polygon</param>
        /// <param name="BorderThickness">Thickness of the outside border</param>>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="Angle">Rotation around the center point in radians</param> 
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param> 
        /// <returns>a copy of the added shape</returns>
        public Polygon AddCenteredPolygon(float Xpos, float Ypos, float Width, float Height, int SideCount, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            //Xpos *= Scale;
            //Ypos *= Scale;
            //Width *= Scale;
            //Height *= Scale;
            Polygon p = new Polygon(new vec2(Xpos, Ypos), new vec2(Width, Height), Angle, SideCount, FillColor, BorderThickness, BorderColor, RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(p);
            return p;
        }
        /// <summary>
        /// Add an image file to the canvas
        /// </summary>
        /// <param name="FilePath">location of the image file</param>
        /// <param name="Xpos">X Coordinate of Sprite center point</param>
        /// <param name="Ypos">Y Coordinate of Sprite center point</param>>
        /// <param name="Width">Width of the Sprite</param>
        /// <param name="Height">Height of the Sprite</param>
        /// <param name="FillColor">Tint of the sprite (white for original color)</param>
        /// <param name="BorderThickness">Thickness of the outside border</param>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="Angle"></param>
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param>
        /// <returns>Reference to the added sprite</returns>
        public Sprite AddCenteredSprite(string FilePath, float Xpos, float Ypos, float Width, float Height, float Angle = 0, float RotationSpeed = 0)
        {
            Sprite s = new Sprite(FilePath, new vec2(Xpos, Ypos), new vec2(Width, Height), Angle, rotationSpeed: RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(s);
            return s;
        }
        /// <summary>
        /// Add an image file to the canvas
        /// </summary>
        /// <param name="FilePath">location of the image file</param>
        /// <param name="XStart">X Coordinate of Start Point</param>
        /// <param name="YStart">Y Coordinate of Start Point</param>>
        /// <param name="Width">Width of the Sprite</param>
        /// <param name="Height">Height of the Sprite</param>
        /// <param name="FillColor">Tint of the sprite (white for original color)</param>
        /// <param name="BorderThickness">Thickness of the outside border</param>
        /// <param name="BorderColor">Color of the outside border</param>
        /// <param name="Angle"></param>
        /// <param name="RotationSpeed">Speed of the rotation animation in radians per frame</param>
        /// <returns>Reference to the added sprite</returns>
        public Sprite AddSprite(string FilePath, float XStart, float YStart, float Width, float Height, Color? Color = null, float Angle = 0, float RotationSpeed = 0)
        {
            Sprite s = new Sprite(FilePath, new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), Angle, rotationSpeed: RotationSpeed)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(s);
            return s;
        }
        /// <summary>
        /// Adds Text in the center of the canvas
        /// </summary>
        /// <param name="text">text to add</param>
        /// <param name="textHeight">size of the tallest letter</param>
        /// <param name="TextColor">color of the text (defaults to white)<</param>
        /// <param name="justification">justification based on the longest line of text</param>
        /// <param name="fontFilepath">path to a truetype font file to use</param>
        /// <returns>a copy of the added shape</returns>
        public Text AddCenteredText(string text, float textHeight, Color ? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath  = "c:\\windows\\fonts\\times.ttf", bool useKerning = false)
        {
            vec2 pos = initializedWithLegacyCoords ? Center : vec2.Zero;
            Text t = new Text(pos, text, textHeight, TextColor == null ? Color.White : TextColor, justification, fontFilepath, useKerning: useKerning)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(t);
            return t;
        }
        /// <summary>
        /// Adds Text Centered anywhere on the canvas
        /// </summary>
        /// <param name="text">text to add</param>
        /// <param name="textHeight">size of the tallest letter</param>
        /// <param name="Xpos">Horizontal center of the text</param>
        /// <param name="Ypos">Vertical center of the text</param>
        /// <param name="TextColor">color of the text (defaults to white)</param>
        /// <param name="justification">justification based on the longest line of text</param>
        /// <param name="fontFilepath">path to a truetype font file to use</param>
        /// <param name="useKerning">Better looking and more accurate at the cost of render speed</param>
        /// <returns>a copy of the added shape</returns>
        public Text AddCenteredText(string text, float textHeight, float Xpos, float Ypos, Color? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath = "c:\\windows\\fonts\\times.ttf", bool useKerning = false)
        {
            Text t = new Text(new vec2(Xpos, Ypos), text, textHeight, TextColor == null ? Color.White : TextColor, justification, fontFilepath, useKerning: useKerning)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(t);
            return t;
        }
        /// <summary>
        /// Adds text to the canvas, linked to and restricted by a bounding rectangle 
        /// </summary>
        /// <param name="text">text to add</param>
        /// <param name="textHeight">size of the tallest letter</param>
        /// <param name="BoundingRect">reference rectangle to restrict te text witin</param>
        /// <param name="TextColor">color of the text (defaults to white)</param>
        /// <param name="justification">justification based on the longest line of text</param>
        /// <param name="fontFilepath">path to a truetype font file to use</param>
        /// <returns>a copy of the added shape</returns>
        //public Text AddText(string text, float textHeight, Rectangle BoundingRect, Color? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath = "c:\\windows\\fonts\\times.ttf", bool useKerning = false)
        //{
        //    Text t = new Text(text, textHeight, TextColor == null ? Color.White : TextColor, justification, fontFilepath, useKerning: useKerning)
        //    {
        //        DrawIndex = nextDrawIndex()
        //    };
        //    AddToBuffer(t);
        //    return t;
        //}
        /// <summary>
        /// Adds text to the canvas, restricted by a described bounding rectangle, the reference to which can be retrieved as a property
        /// </summary>
        /// <param name="text">text to add</param>
        /// <param name="textHeight">size of the tallest letter</param>
        /// <param name="XStart">X start position of the bounding box</param>
        /// <param name="YStart">Y starat position of the bounding box</param>
        /// <param name="width">width of the bounding box</param>
        /// <param name="height">heigt of te bounding box</param>
        /// <param name="TextColor">color of the text (defaults to white)</param>
        /// <param name="justification">justification based on the longest line of text</param>
        /// <param name="fontFilepath">path to a truetype font file to use</param>
        /// <returns>a copy of the added shape</returns>
        public Text AddText(string text, float textHeight, int XStart, int YStart, int width, int height, Color? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath = "c:\\windows\\fonts\\times.ttf", bool useKerning = false)
        {
            // Rectangle BoundingRect = new Rectangle(new vec2(XStart + width, YStart + height) / 2f, new vec2(width, height));
            Text t = new Text(new vec2(XStart + width / 2, YStart + height / 2), new vec2(width, height), text, textHeight, TextColor == null ? Color.White : TextColor, justification, fontFilepath, useKerning: useKerning)
            {
                DrawIndex = nextDrawIndex()
            };
            AddToBuffer(t);
            return t;
        }

        public Shape Add(Shape shape)
        {
            if (shape == null && !shapeRefs.Contains(shape))
                throw new NullReferenceException("Shape was NULL");
            AddToBuffer(shape);
            return shape;
        }
        public GameObject Add(GameObject gameObject)
        {
            if (gameObject == null && !GORefs.Contains(gameObject))
                throw new NullReferenceException("GameObject was NULL");
            GLWrapper.addGO(gameObject.internalGO);
            GORefs.Add(gameObject);
            gameObject.can = this;

            EarlyUpdate += gameObject.InternalEarlyUpdate;
            Update += gameObject.InternalUpdate;
            LateUpdate += gameObject.InternalLateUpdate;

            return gameObject;
        }
        public GameObject Instantiate(GameObject original)
        {
            GameObject clone = original.Clone();
            Add(clone);
            clone.updateInternals(); //Updates the internal gameobject values
            return clone;
        }
        public GameObject Instantiate(GameObject original, vec2 position, float rotation = 0)
        {
            GameObject clone = Instantiate(original);
            clone.transform.Position = position;
            clone.transform.Rotation = rotation;
            clone.updateInternals(); //Updates the internal gameobject values
            return clone;
        }
        public GameObject Instantiate(GameObject original, vec2 position, float rotation, GameObject parent)
        {
            GameObject clone = Instantiate(original, position, rotation);
            clone.Parent = parent;
            clone.updateInternals(); //Updates the internal gameobject values
            return clone;
        }

        public bool RayCast(vec2 start, vec2 end)
        {
            if (start == end)
                return false;
            return GLWrapper.raycast(start.x, start.y, end.x, end.y);
        }

        /// <summary>Loads image in to memory emediatly. Can prevent stuttering when trying to use an unloaded image at runtime</summary
        public void LoadAsset(string filePath)
        {
            GLWrapper.loadImageAsset(filePath);
        }
        public void LoadAssets(string[] filePath, bool showLoadingScreen)
        {
            foreach (string s in filePath)
            {
                GLWrapper.loadImageAsset(s);
            }
        }

        //// <summary>renders all shapes to the screen</summary>
        public void Render() => renderNextFrame = true;

        /// <summary>stops drawaing a shape on the canvas</summary
        public void Remove(Shape s)
        {
            if (s == null)
                throw new NullReferenceException("Shape was null");
            AddToRemoveBuffer(s);
        }
        public void Remove(GameObject gameObject)
        {
            GORefs.Remove(gameObject);           
            GLWrapper.removeGO(gameObject.internalGO);
            disposeBuffer.Add(gameObject.Dispose);
        }

        /// <summary>displays a shape one index behind other shapes on the canvas</summary>
        public void SendBackward(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalGO))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            shapeRefs.Select(o => o.DrawIndex--);
            shape.DrawIndex++;

            //Old system method:
            //int shapeIndex = GLWrapper.getRectIndex(shape.internalGO);
            //if (shapeIndex > 0)
            //    GLWrapper.swapOrder(shapeIndex, shapeIndex - 1);
        }

        /// <summary>displays a shape one index in front of the other shapes on the canvas</summary>
        public void SendForward(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalGO))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            shapeRefs.Select(o => o.DrawIndex++);
            shape.DrawIndex--;

            //Old system method:
            //int shapeIndex = GLWrapper.getRectIndex(shape.internalGO);
            //if(shapeIndex < ShapeCount -1)
            //    GLWrapper.swapOrder(shapeIndex, shapeIndex +1);
        }

        /// <summary>sets a shape to be drawn behind every other shape on the canvas</summary>
        public void SendToBack(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalGO))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            shape.DrawIndex = shapeRefs.Max(o => o.DrawIndex) + 1;

            //Old system method:
            //int shapeIndex = GLWrapper.getRectIndex(shape.internalGO);
            //for (int i = shapeIndex; i > 0; i--)
            //    GLWrapper.swapOrder(i, i - 1);
        }

        /// <summary>sets a shape to be drawn in front of every other shape on the canvas</summary>
        public void SendToFront(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalGO))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");
 
            shape.DrawIndex = shapeRefs.Min(o => o.DrawIndex) -1;

            //Old system method:
            //int max = ShapeCount; //CLR properties are slow
            //int shapeIndex = GLWrapper.getRectIndex(shape.internalGO);

            //for (int i = shapeIndex; i < max - 1; i++)
            //    GLWrapper.swapOrder(i, i + 1);
        }

        //NOTE This no longer works due to a change which allows shapes with the same draw order index
        /// <summary>
        /// swaps the drawing order of two shapes and which will apear in front of the other
        /// </summary>
        /// <param name="IndexA">order of the first shape is drawn to the canvas</param>
        /// <param name="IndexB">order of the second shape is drawn to the canvas</param>
        //public void SwapDrawOrder(int IndexA, int IndexB)
        //{
        //    int max = ShapeCount; //CLR properties are slow
        //    if (IndexA > max || IndexA < 0)
        //        throw new ArgumentOutOfRangeException("IndexA", IndexA, "Shape index was out of canvas range (0 - " + max + ")");
        //    if (IndexB > max || IndexB < 0)
        //        throw new ArgumentOutOfRangeException("IndexB", IndexB, "Shape index was out of canvas range (0 - " + max + ")");

        //    //GLWrapper.swapOrder(IndexA, IndexB);
        //}

        /// <summary>swaps the drawing order of two shapes and which will apear in front of the other</summary>
        public void SwapDrawOrder(Shape shapeA, Shape shapeB)
        {
            if (!GLWrapper.checkLoaded(shapeA.internalGO))
                throw new ArgumentException("Shape A was not found on / wasn't added to the canvas", "shapeA");
            if (!GLWrapper.checkLoaded(shapeB.internalGO))
                throw new ArgumentException("Shape B was not found on / wasn't added to the canvas", "shapeB");

            int temp = shapeA.DrawIndex;
            shapeA.DrawIndex = shapeB.DrawIndex;
            shapeB.DrawIndex = temp;

            //GLWrapper.swapOrder(GLWrapper.getRectIndex(shapeA.internalGO), GLWrapper.getRectIndex(shapeB.internalGO));
        }

        /// <summary>
        /// Calls a function after a number of seconds
        /// </summary>
        /// <param name="function">Function to be called</param>
        /// <param name="time">Time is seconds before function is called</param>
        public void Invoke(Action function, float time = 0)
        {
            if (function != null)
                delayedCalls.Add(new DelayedCall(function, time));
        }
        /// <summary>
        /// Calls a function after a number of seconds
        /// </summary>
        /// <param name="function">Function to be called</param>
        /// <param name="time">Time is seconds before function is called</param>
        public void InvokeRepeating(Action function, float time)
        {
            if (function != null)
                delayedCalls.Add(new DelayedCall(function, time, true));
        }

        private class DelayedCall
        {
            public Action func;
            public float timeLeft;
            public float initialTime;
            public bool repeating;
            public DelayedCall(Action f, float t, bool repeat = false)
            {
                func = f;
                timeLeft = t;
                initialTime = t;
                repeating = repeat;
            }
        }

        /// <summary>Writes all the pixels on the canvas to a BMP image file</summary>
        public void WriteToBMP (string filepath)
        {
            if (!Regex.IsMatch(filepath, @"\.bmp$"))
                filepath += ".bmp";
            Invoke(delegate { GLWrapper.saveCanvasAsImage(filepath); });
        }

        /// <summary>sets the color of a single pixel on theh back buffer</summary>
        public void SetBBPixel(int x, int y, Color color)
        { 
            if (x < 0 || x >= Width) 
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y >= Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");
            GLWrapper.setBBpixel(x, y, color.R, color.G, color.B, color.A);
        }

        /// <summary>sets a back buffer pixel faser but without error checking or blending. Not Thread Safe!</summary>
        public void SetBBPixelFast(int x, int y, Color color)
        {
            GLWrapper.setBBpixelFast(x, y, color.R, color.G, color.B, color.A);
        }

        /// <summary>draws a whole shape to the back buffer</summary>
        public Shape SetBBShape(Shape shape)
        {
            if (shape == null)
                throw new NullReferenceException("Shape was null");
            GLWrapper.setBBShape(shape.internalGO);
            return shape;
        }
        public void tempF() => GLWrapper.tempF();
        /// <summary>gets the color of a single pixel on the canvas</summary>
        public Color GetPixel(vec2 pixel) => GLWrapper.getPixel((int)pixel.x, (int)pixel.y);
        /// <summary>gets the color of a single pixel on the canvas</summary>
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x > Width)
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y > Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");

            return GLWrapper.getPixel(x, y);
        }

        /// <summary>
        /// sets the upper-left corner location of the window on the screen 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetWindowPosition(int x, int y)
        {
            if (x < 0)
                throw new ArgumentException("X coordinate must be a positive number", "x");
            if (y < 0)
                throw new ArgumentException("Y coordinate must be a positive number", "y");
            GLWrapper.setPos(x, y);
        }

        /// <summary>closes the canvas</summary>
        public void Close()
        {
            GLWrapper.close();
        }
        /// <summary>sets every pixel on the back buffer to the back buffer color</summary>
        public void ClearBackBuffer() => GLWrapper.clearBB();
        /// <summary>removes all shapes from the canvas</summary>
        public void Clear()
        {         

            Invoke(delegate
            {
                GLWrapper.clearShapes();
                GORefs.ForEach(g => g.Destroy());
                GORefs.Clear();
                shapeRefs.Clear();
            });
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                GLWrapper.close();
                if (disposing)
                {
                    //if any managed resources need to be disposed, it should be done here
                }        
            }
        }
        ~GLCanvas()
        {
            Dispose(false);
        }

        /// <summary>Configures the camera to be like GDIDrawer with (0,0) in the top left</summary>
        public void SetInvertedCoordinates()
        {
            CameraScale = new vec2(1, -1);
            CameraPosition = Center;
        }
        /// <summary>Configures the camera to it's original cartesian system</summary>
        public void SetCartesianCoordinates()
        {
            CameraScale = new vec2(1, 1);
            CameraPosition = vec2.Zero;
        }

        //Extra, user end functions

        /// <summary>
        /// Creates a Trigonometric sawtooth wave over time
        /// </summary>
        public static float Saw(float t)
        {
            return (float)Math.Asin(Math.Sin(t));
        }
        /// <summary>
        /// Goes back and forth between two values over time
        /// </summary>
        public static float Pingpong(float t, float length)
        {
            return (float)(Math.Acos(Math.Cos(t * Math.PI * (1f / 1000f))) / (Math.PI * (1f / length))); //acos(cos(t*pi*(1/1000)))/pi * 1/length
        }
        /// <summary>
        /// Goes back and forth between two values trigonometricly 
        /// </summary>
        public static float PingpongSmooth(float t, float length)
        {
            return (float)((-Math.Cos(t * Math.PI * (1f / 1000f))) / (2 * (1 / length))) + length / 2f;
        }
        /// <summary>
        /// Linear interpolates between two values
        /// </summary>
        public static float Lerp(float first, float second, float time)
        {
            return first * time + second * (1 - time);
        }
        //ping pongs: https://www.desmos.com/calculator/bq4qwfxb0e sub t for 1000 and x for t
    }

    //used for diagnostic purposes only
    internal struct Pixel
    {
        public int x, y;
        public Color color;
        public static int count = 0;

        public Pixel(int x, int y, Color color)
        {
            this.x = x;
            this.y = y;
            this.color = color;
            Interlocked.Increment(ref count);
        }
    }
}

