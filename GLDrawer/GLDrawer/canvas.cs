using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class GLCanvas
    {
        private bool usePackedShaders = true; //set this as true to compile the last packed version of the shaders into the executable/DLL (see shaders.h for more info)

        private float iTime, iDeltaTime, lastTime;
        /// <summary>time in seconds since canvas creation</summary>
        public float Time { get => iTime; }
        /// <summary>time in seconds of the last frame time</summary>
        public float DeltaTime { get => iDeltaTime; }
        /// <summary>text to display on the window title</summary>
        public string WindowTitle { get => GLWrapper.title; set => GLWrapper.title = value; }
        /// <summary>wether 0 on the y axis starts from the top or bottom</summary>
        private bool InvertedYAxis = false; //unimplimented for public use as of now
        /// <summary>multiplier for all shape coordinates</summary>
        public int Scale = 1;
        /// <summary>wether to display debug info next to the title</summary>
        public bool ExtraInfo { set => GLWrapper.titleDetails = value; }
        /// <summary>center coordinate of the canvas</summary>
        public vec2 Centre { get => new vec2(Width / 2, Height / 2); }
        public vec2 Camera = vec2.Zero;
        /// <summary>number of shapes being drawn on the canvas</summary>
        public int ShapeCount { get => GLWrapper.shapeCount; }
        public bool VSync { get; private set; } //these four are set at initialization
        public bool DebugMode { get; private set; }
        public bool Borderless { get; private set; }
        public bool AutoRender { get; private set; }
        /// <summary>ignors back buffer tools and keeps it a solid color</summary>
        public bool simpleBackBuffer = false;
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

        /// <summary>
        /// an event which occurs once per frame update
        /// </summary>
        public event Action Update = delegate { };


        private unmanaged_Canvas GLWrapper;
        private Form iform;
        private Panel ipanel;
        private bool embedded = false;
        private bool initialized;
        private bool NullRemovalFlag = false; //used for thread safe garbage collection
        private bool frozen = false; //used to emergency freeze the main thread in case of runtime memory alocation such as a window resize
        private bool renderNextFrame = true; //tracks manual rendering
        private List<Shape> shapeRefs = new List<Shape>();

        private static List<GLCanvas> activeCanvases = new List<GLCanvas>();
        private static Thread loopthread;
        private static Thread mainThread;
        private static bool threadsInitialized = false;

        private void startCanvas(GLCanvas canvas)
        {
            activeCanvases.Add(canvas);
            if (!threadsInitialized)
            {
                mainThread = Thread.CurrentThread;
                threadsInitialized = true;
                loopthread = new Thread(new ThreadStart(threadLoop));

                loopthread.Start();
            }

            //it's a bad idea to continue before the canvas is initialized
            while (GLWrapper == null || !GLWrapper.initialized)
            {
                Thread.Sleep(1);
            }
        }
        private static void threadLoop()
        {
            while (true)
            {
                //if the end of the main program is reached, all the canvas windows should close
                if (!mainThread.IsAlive)
                    for (int i = 0; i < activeCanvases.Count; i++)
                        activeCanvases[i].GLWrapper.shouldClose = true;

                //when all canvasas are closed, the thread is aborted
                if (activeCanvases.Count == 0)
                    Thread.CurrentThread.Abort();

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
                    if (can.GLWrapper.shouldClose)
                    {
                        activeCanvases.RemoveAt(i);
                        continue;
                    }
                    can.mainLoop();
                }
            }
        }

        /// <summary>
        /// Creates a new window with a GLCanvas
        /// </summary>
        /// <param name="width">Width of the window</param>
        /// <param name="height">Height of the window</param>
        /// <param name="title">Name of the window title</param>
        /// <param name="BackColor">Background color of the canvas</param>
        /// <param name="TitleDetails">Displays render time, FPS, and shape count in the title</param>
        /// <param name="VSync">Limits the framerate to 60fps and waits for vertical screen synchronization. Set to false for uncapped framerate</param>
        /// <param name="autoRender">wether to automatically render objects on the canvas each frame</param>
        /// <param name="debugMode">Display rendering information on top of the canvas</param>
        /// <param name="borderless">Wether or not the window is borderless</param>
        public GLCanvas(int width = 800, int height = 600, string title = "Canvas Window", Color? BackColor = null, bool TitleDetails = true, bool VSync = true, bool autoRender = true, bool debugMode = false, bool borderless = false)
        {
            GLWrapper = new unmanaged_Canvas(usePackedShaders);
            GLWrapper.title = title;
            GLWrapper.titleDetails = TitleDetails;
            iWidth = width;
            iHeight = height;
            this.VSync = activeCanvases.Count > 0 ? false : VSync; //can't have more than one context with vsync, or it will run at 30fps
            Borderless = borderless;
            DebugMode = debugMode;
            iBackColor = BackColor == null ? Color.Black : (Color)BackColor;
            AutoRender = autoRender;
            renderNextFrame = autoRender;

            startCanvas(this);
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

            startCanvas(this);
        }
        //C++ backend needs to know where to trigger input events
        private void Initialize()
        {
            GLWrapper.Input.setMouseCallback(MouseCallback);
            GLWrapper.Input.setKeyCallback(KeyCallback);
            GLWrapper.Input.setMouseMoveCallback(MouseMoveCallback);
            initialized = true;
        }
        private void mainLoop()
        {
            iTime = GLWrapper.ellapsedTime;
            iDeltaTime = iTime - lastTime;
            lastTime = iTime;

            Update.Invoke();
            MouseScrollDirection = 0;

            if (NullRemovalFlag)
            {
                GLWrapper.clearNullRects();
                GC.Collect();
                NullRemovalFlag = false;
            }

            if (simpleBackBuffer)
                GLWrapper.clearBB();

            //needs to be very spesific due to threads
            if (!AutoRender)
            {
                if (renderNextFrame)
                {
                    GLWrapper.mainloop(true);
                    renderNextFrame = false;
                }
                else
                    GLWrapper.mainloop(false);
            }
            else
                GLWrapper.mainloop(true);

            iWidth = GLWrapper.width;
            iHeight = GLWrapper.height;
            frozen = false;
        }

        //shortcut for converting null colors to transparent colors for default parameters
        private Color checkNullC(Color ? c)
        {
            return c == null ? Color.Invisible : (Color)c;
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
        public Rectangle AddRectangle(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            XStart *= Scale;
            YStart *= Scale;
            Width *= Scale;
            Height *= Scale;
            Rectangle r = new Rectangle(new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(r.internalShape);
            shapeRefs.Add(r);
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
        public Rectangle AddCenteredRectangle(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Xpos *= Scale;
            Ypos *= Scale;
            Width *= Scale;
            Height *= Scale;
            Rectangle r = new Rectangle(new vec2(Xpos, Ypos), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(r.internalShape);
            shapeRefs.Add(r);
            return r;
        }
        /// <summary>
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
        public Ellipse AddEllipse(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            XStart *= Scale;
            YStart *= Scale;
            Width *= Scale;
            Height *= Scale;
            Ellipse e = new Ellipse(new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(e.internalShape);
            shapeRefs.Add(e);
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
        public Ellipse AddCenteredEllipse(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Xpos *= Scale;
            Ypos *= Scale;
            Width *= Scale;
            Height *= Scale;
            Ellipse e = new Ellipse(new vec2(Xpos, Ypos), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(e.internalShape);
            shapeRefs.Add(e);
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
            XStart *= Scale;
            YStart *= Scale;
            XEnd *= Scale;
            YEnd *= Scale;
            Line l = new Line(new vec2(XStart, YStart), new vec2(XEnd, YEnd), Thickness, LineColor, BorderThickness, BorderColor, RotationSpeed);
            GLWrapper.addRect(l.internalShape);
            shapeRefs.Add(l);
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
            StartPos *= Scale;
            Length *= Scale;
            Line l = new Line(StartPos, Length, Thickness, Angle, LineColor, BorderThickness, BorderColor, RotationSpeed);
            GLWrapper.addRect(l.internalShape);
            shapeRefs.Add(l);
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
            Xpos *= Scale;
            Ypos *= Scale;
            Width *= Scale;
            Height *= Scale;
            Polygon p = new Polygon(new vec2(Xpos, Ypos), new vec2(Width, Height), SideCount, FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(p.internalShape);
            shapeRefs.Add(p);
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
        public Sprite AddCenteredSprite(string FilePath, float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Xpos *= Scale;
            Ypos *= Scale;
            Width *= Scale;
            Height *= Scale;
            Sprite s = new Sprite(FilePath, new vec2(Xpos, Ypos), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(s.internalShape);
            shapeRefs.Add(s);
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
        public Sprite AddSprite(string FilePath, float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            XStart *= Scale;
            YStart *= Scale;
            Width *= Scale;
            Height *= Scale;
            Sprite s = new Sprite(FilePath, new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            GLWrapper.addRect(s.internalShape);
            shapeRefs.Add(s);
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
        public Text AddCenteredText(string text, float textHeight, Color ? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath  = "c:\\windows\\fonts\\times.ttf")
        {
            Text t = new Text(this.Centre, text, textHeight, TextColor == null ? Color.White : TextColor, justification, fontFilepath);
            GLWrapper.addRect(t.internalShape);
            shapeRefs.Add(t);
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
        /// <returns>a copy of the added shape</returns>
        public Text AddCenteredText(string text, float textHeight, float Xpos, float Ypos, Color? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath = "c:\\windows\\fonts\\times.ttf")
        {
            Xpos *= Scale;
            Ypos *= Scale;
            Text t = new Text(new vec2(Xpos, Ypos), text, textHeight, TextColor == null ? Color.White : TextColor, justification, fontFilepath);
            GLWrapper.addRect(t.internalShape);
            shapeRefs.Add(t);
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
        public Text AddText(string text, float textHeight, Rectangle BoundingRect, Color? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath = "c:\\windows\\fonts\\times.ttf")
        {
            Text t = new Text(text, textHeight, BoundingRect, TextColor == null ? Color.White : TextColor, justification, fontFilepath);
            GLWrapper.addRect(t.internalShape);
            shapeRefs.Add(t);
            return t;
        }
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
        public Text AddText(string text, float textHeight, int XStart, int YStart, int width, int height, Color? TextColor = null, JustificationType justification = JustificationType.Center, string fontFilepath = "c:\\windows\\fonts\\times.ttf")
        {
            XStart *= Scale;
            YStart *= Scale;
            Width *= Scale;
            Height *= Scale;
            Rectangle BoundingRect = new Rectangle(new vec2(XStart + width, YStart + height) / 2f, new vec2(width, height));
            Text t = new Text(text, textHeight, BoundingRect, TextColor == null ? Color.White : TextColor, justification, fontFilepath);
            GLWrapper.addRect(t.internalShape);
            shapeRefs.Add(t);
            return t;
        }

        public Shape Add(Shape shape)
        {
            GLWrapper.addRect(shape.internalShape);
            shapeRefs.Add(shape);
            return shape;
        }
        //// <summary>renders all shapes to the screen</summary>
        public void Render() => renderNextFrame = true;
        /// <summary>stops drawaing a shape on the canvas</summary>
        public void RemoveShape(Shape s)
        {
            shapeRefs.Remove(s);
            GLWrapper.removeRect(s.internalShape);
        }
        /// <summary>displays a shape one index behind other shapes on the canvas</summary>
        public void SendBackward(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalShape))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int shapeIndex = GLWrapper.getRectIndex(shape.internalShape);
            if (shapeIndex > 0)
                GLWrapper.swapOrder(shapeIndex, shapeIndex - 1);
        }
        /// <summary>displays a shape one index in front of the other shapes on the canvas</summary>
        public void SendForward(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalShape))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int shapeIndex = GLWrapper.getRectIndex(shape.internalShape);
            if(shapeIndex < ShapeCount -1)
                GLWrapper.swapOrder(shapeIndex, shapeIndex +1);
        }
        /// <summary>sets a shape to be drawn behind every other shape on the canvas</summary>
        public void SendToBack(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalShape))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int shapeIndex = GLWrapper.getRectIndex(shape.internalShape);
            for (int i = shapeIndex; i > 0; i--)
                GLWrapper.swapOrder(i, i - 1);
        }
        /// <summary>sets a shape to be drawn in front of every other shape on the canvas</summary>
        public void SendToFront(Shape shape)
        {
            if (!GLWrapper.checkLoaded(shape.internalShape))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int max = ShapeCount; //CLR properties are slow
            int shapeIndex = GLWrapper.getRectIndex(shape.internalShape);

            for (int i = shapeIndex; i < max-1; i++)
                GLWrapper.swapOrder(i, i + 1);
        }
        /// <summary>
        /// swaps the drawing order of two shapes and which will apear in front of the other
        /// </summary>
        /// <param name="IndexA">order of the first shape is drawn to the canvas</param>
        /// <param name="IndexB">order of the second shape is drawn to the canvas</param>
        public void SwapDrawOrder(int IndexA, int IndexB)
        {
            int max = ShapeCount; //CLR properties are slow
            if (IndexA > max || IndexA < 0)
                throw new ArgumentOutOfRangeException("IndexA", IndexA, "Shape index was out of canvas range (0 - " + max + ")");
            if (IndexB > max || IndexB < 0)
                throw new ArgumentOutOfRangeException("IndexB", IndexB, "Shape index was out of canvas range (0 - " + max + ")");

            GLWrapper.swapOrder(IndexA, IndexB);
        }
        /// <summary>swaps the drawing order of two shapes and which will apear in front of the other</summary>
        public void SwapDrawOrder(Shape shapeA, Shape shapeB)
        {
            if (!GLWrapper.checkLoaded(shapeA.internalShape))
                throw new ArgumentException("Shape A was not found on / wasn't added to the canvas", "shapeA");
            if (!GLWrapper.checkLoaded(shapeB.internalShape))
                throw new ArgumentException("Shape B was not found on / wasn't added to the canvas", "shapeB");
            GLWrapper.swapOrder(GLWrapper.getRectIndex(shapeA.internalShape), GLWrapper.getRectIndex(shapeB.internalShape));
        }
        /// <summary>sets the color of a single pixel on theh back buffer</summary>
        public void SetBBPixel(int x, int y, Color color)
        { 
            if (x < 0 || x > Width) 
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y > Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");
            GLWrapper.setBBpixel(x, y, color);
        }
        /// <summary>draws a whole shape to the back buffer</summary>
        public Shape SetBBShape(Shape shape)
        {
            GLWrapper.setBBShape(shape.internalShape);
            return shape;
        }
        /// <summary>gets the color of a single pixel on the canvas</summary>
        public Color getPixel(vec2 pixel) => GLWrapper.getPixel((int)pixel.x, (int)pixel.y);
        /// <summary>gets the color of a single pixel on the canvas</summary>
        public Color getPixel(int x, int y)
        {
            if (x < 0 || x > Width)
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y > Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");

            return GLWrapper.getPixel(x, y);
        }


        /// <summary>removes shapes with now references from the canvas</summary>
        public void Refresh() => NullRemovalFlag = true;

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
            GLWrapper.shouldClose = true;
            //GLWrapper.dispose();
        }
        /// <summary>sets every pixel on the back buffer to the back buffer color</summary>
        public void ClearBackBuffer() => GLWrapper.clearBB();
        /// <summary>removes all shapes from the canvas</summary>
        public void Clear()
        {
            shapeRefs.Clear();
            GLWrapper.clearShapes();
        }
        ~GLCanvas()
        {
            GLWrapper.dispose();
        }
    }

  
    public static class GMath
    {
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
        public static float lerp(float first, float second, float time)
        {
            return first * time + second * (1 - time);
        }
        //ping pongs: https://www.desmos.com/calculator/bq4qwfxb0e sub t for 1000 and x for t
    }
}

