using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GL3DrawerCLR;
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
        public string WindowTitle { get => gldw.title; set => gldw.title = value; }
        /// <summary>wether 0 on the y axis starts from the top or bottom</summary>
        private bool InvertedYAxis = false; //unimplimented for public use as of now
        /// <summary>multiplier for all shape coordinates</summary>
        public int Scale = 1;
        /// <summary>wether to display debug info next to the title</summary>
        public bool ExtraInfo { set => gldw.titleDetails = value; }
        /// <summary>center coordinate of the canvas</summary>
        public vec2 Centre { get => new vec2(Width / 2, Height / 2); }
        /// <summary>number of shapes being drawn on the canvas</summary>
        public int ShapeCount { get => gldw.shapeCount; }
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
                if(gldw != null)
                {
                    gldw.backColor = value;
                    gldw.clearBB();
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
                gldw.width = value;
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
                gldw.height = value;
                frozen = true;
                while (frozen) { }
            }
        }

        /// <summary>
        /// an event which occurs once per frame update
        /// </summary>
        public event Action Update = delegate { };


        private GLDWrapper gldw;
        private Form iform;
        private Panel ipanel;
        private bool embedded = false;
        private bool initialized;
        private bool NullRemovalFlag = false; //used for thread safe garbage collection
        private bool frozen = false; //used to emergency freeze the main thread in case of runtime memory alocation such as a window resize
        private bool renderNextFrame = true; //tracks manual rendering

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
            while (gldw == null || !gldw.initialized)
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
                        activeCanvases[i].gldw.shouldClose = true;

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
                            can.gldw.createCanvas(can.iWidth, can.iHeight, true, can.BackColor, true, can.DebugMode);
                            SetParent(can.gldw.getNativeHWND(), can.panelHandle);
                        }
                        else
                            can.gldw.createCanvas(can.iWidth, can.iHeight, can.Borderless, can.BackColor, can.VSync, can.DebugMode);
                        can.Initialize();
                    }
                    if (can.gldw.shouldClose)
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
            gldw = new GLDWrapper(usePackedShaders);
            gldw.title = title;
            gldw.titleDetails = TitleDetails;
            iWidth = width;
            iHeight = height;
            this.VSync = VSync;
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
            gldw = new GLDWrapper(usePackedShaders);
            iHeight = panel.Height;
            iWidth = panel.Width;
            form.FormClosed += delegate { Close(); }; //close the canvas when the parent form is closed
            panelHandle = panel.Handle;
            this.VSync = true;
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
            gldw.Input.setMouseCallback(MouseCallback);
            gldw.Input.setKeyCallback(KeyCallback);
            gldw.Input.setMouseMoveCallback(MouseMoveCallback);
            initialized = true;
        }
        private void mainLoop()
        {
            iTime = gldw.ellapsedTime;
            iDeltaTime = iTime - lastTime;
            lastTime = iTime;

            Update.Invoke();
            MouseScrollDirection = 0;

            if (NullRemovalFlag)
            {
                gldw.clearNullRects();
                GC.Collect();
                NullRemovalFlag = false;
            }

            if (simpleBackBuffer)
                gldw.clearBB();

            //needs to be very spesific due to threads
            if (!AutoRender)
            {
                if (renderNextFrame)
                {
                    gldw.mainloop(true);
                    renderNextFrame = false;
                }
                else
                    gldw.mainloop(false);
            }
            else
                gldw.mainloop(true);

            iWidth = gldw.width;
            iHeight = gldw.height;
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
            gldw.addRect(r.rect);
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
            gldw.addRect(r.rect);
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
            gldw.addRect(e.rect);
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
            gldw.addRect(e.rect);
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
            gldw.addRect(l.rect);
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
            gldw.addRect(l.rect);
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
            gldw.addRect(p.rect);
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
            gldw.addRect(s.rect);
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
            gldw.addRect(s.rect);
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
            gldw.addRect(t.rect);
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
            gldw.addRect(t.rect);
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
            gldw.addRect(t.rect);
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
            gldw.addRect(t.rect);
            return t;
        }

        public Shape Add(Shape shape)
        {
            gldw.addRect(shape.rect);
            return shape;
        }
        //// <summary>renders all shapes to the screen</summary>
        public void Render() => renderNextFrame = true;
        /// <summary>stops drawaing a shape on the canvas</summary>
        public void RemoveShape(Shape s) => gldw.removeRect(s.rect);
        /// <summary>displays a shape one index behind other shapes on the canvas</summary>
        public void SendBackward(Shape shape)
        {
            if (!gldw.checkLoaded(shape.rect))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int shapeIndex = gldw.getRectIndex(shape.rect);
            if (shapeIndex > 0)
                gldw.swapOrder(shapeIndex, shapeIndex - 1);
        }
        /// <summary>displays a shape one index in front of the other shapes on the canvas</summary>
        public void SendForward(Shape shape)
        {
            if (!gldw.checkLoaded(shape.rect))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int shapeIndex = gldw.getRectIndex(shape.rect);
            if(shapeIndex < ShapeCount -1)
                gldw.swapOrder(shapeIndex, shapeIndex +1);
        }
        /// <summary>sets a shape to be drawn behind every other shape on the canvas</summary>
        public void SendToBack(Shape shape)
        {
            if (!gldw.checkLoaded(shape.rect))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int shapeIndex = gldw.getRectIndex(shape.rect);
            for (int i = shapeIndex; i > 0; i--)
                gldw.swapOrder(i, i - 1);
        }
        /// <summary>sets a shape to be drawn in front of every other shape on the canvas</summary>
        public void SendToFront(Shape shape)
        {
            if (!gldw.checkLoaded(shape.rect))
                throw new ArgumentException("Shape was not found on / wasn't added to the canvas", "shape");

            int max = ShapeCount; //CLR properties are slow
            int shapeIndex = gldw.getRectIndex(shape.rect);

            for (int i = shapeIndex; i < max-1; i++)
                gldw.swapOrder(i, i + 1);
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

            gldw.swapOrder(IndexA, IndexB);
        }
        /// <summary>swaps the drawing order of two shapes and which will apear in front of the other</summary>
        public void SwapDrawOrder(Shape shapeA, Shape shapeB)
        {
            if (!gldw.checkLoaded(shapeA.rect))
                throw new ArgumentException("Shape A was not found on / wasn't added to the canvas", "shapeA");
            if (!gldw.checkLoaded(shapeB.rect))
                throw new ArgumentException("Shape B was not found on / wasn't added to the canvas", "shapeB");
            gldw.swapOrder(gldw.getRectIndex(shapeA.rect), gldw.getRectIndex(shapeB.rect));
        }
        /// <summary>sets the color of a single pixel on theh back buffer</summary>
        public void SetBBPixel(int x, int y, Color color)
        { 
            if (x < 0 || x > Width) 
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y > Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");
            gldw.setBBpixel(x, y, color);
        }
        /// <summary>draws a whole shape to the back buffer</summary>
        public Shape SetBBShape(Shape shape)
        {
            gldw.setBBShape(shape.rect);
            return shape;
        }
        /// <summary>gets the color of a single pixel on the canvas</summary>
        public Color getPixel(vec2 pixel) => gldw.getPixel((int)pixel.x, (int)pixel.y);
        /// <summary>gets the color of a single pixel on the canvas</summary>
        public Color getPixel(int x, int y)
        {
            if (x < 0 || x > Width)
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y > Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");

            return gldw.getPixel(x, y);
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
            gldw.setPos(x, y);
        }

        /// <summary>closes the canvas</summary>
        public void Close()
        {
            gldw.shouldClose = true;
            //gldw.dispose();
        }
        /// <summary>sets every pixel on the back buffer to the back buffer color</summary>
        public void ClearBackBuffer() => gldw.clearBB();
        /// <summary>removes all shapes from the canvas</summary>
        public void Clear() => gldw.clearShapes();
        ~GLCanvas()
        {
            gldw.dispose();
        }
    }

    public struct vec2
    {
        public float x, y;

        public vec2(float X, float Y)
        {
            x = X;
            y = Y;
        }
        public vec2(float XY)
        {
            x = XY;
            y = XY;
        }

        public static vec2 Zero { get { return new vec2(0); } }
        /// <summary>vec2 withlargest possible values</summary>
        public static vec2 Max { get { return new vec2(float.MaxValue); } }
        /// <summary>vec2 with abolute values of x and y</summary>
        public vec2 Abs { get { return new vec2(Math.Abs(x), Math.Abs(y)); } }

        //implicit vec2 to Gl3DrawerCLR vec2 convertion which is implcitly converted to glm vec2 internally
        public static implicit operator GL3DrawerCLR.vec2(vec2 v)
        {
            return new GL3DrawerCLR.vec2(v.x, v.y);
        }

        /// <summary>gets the distance from the target</summary>
        public float Length(vec2 Target)
        {
            float a = x - Target.x;
            float b = y - Target.y;
            return (float)Math.Sqrt(a*a + b*b);
        }

        public override bool Equals(Object obj)
        {
            return obj is vec2 && this == (vec2)obj;
        }
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }
        /// <summary>returns a directional vector from 0,0 with a radius of 1.0</summary>
        public vec2 Normalize()
        {
            float distance = (float)Math.Sqrt(this.x * this.x + this.y * this.y);
            return new vec2(this.x / distance, this.y / distance);
        }
        /// <summary>linear interpolation between vectors</summary>
        public vec2 lerp(vec2 target, float time)
        {
            float retX = x * time + target.x * (1 - time);
            float retY = y * time + target.y * (1 - time);
            return new vec2(retX, retY);
        }
        //implicit vec2 to PointF
        public static implicit operator System.Drawing.PointF(vec2 v)
        {
            return new System.Drawing.PointF(v.x, v.y);
        }
        //implicit PointF to vec2
        public static implicit operator vec2(System.Drawing.PointF p)
        {
            return new vec2(p.X, p.Y);
        }
        //implicit vec2 to Point
        public static implicit operator System.Drawing.Point(vec2 v)
        {
            return new System.Drawing.Point((int)v.x, (int)v.y);
        }
        //implicit Point to vec2
        public static implicit operator vec2(System.Drawing.Point p)
        {
            return new vec2(p.X, p.Y);
        }
        //equals
        public static bool operator ==(vec2 a, vec2 b)
        {
            return a.x == b.x && a.y == b.y;
        }
        //does not equal
        public static bool operator !=(vec2 a, vec2 b)
        {
            return !(a == b);
        }
        //addition
        public static vec2 operator +(vec2 a, vec2 b)
        {
            return new vec2(a.x + b.x, a.y + b.y);
        }
        //subtraction
        public static vec2 operator -(vec2 a, vec2 b)
        {
            return new vec2(a.x - b.x, a.y - b.y);
        }
        //multiplication
        public static vec2 operator *(vec2 a, vec2 b)
        {
            return new vec2(a.x * b.x, a.y * b.y);
        }
        //division
        public static vec2 operator /(vec2 a, vec2 b)
        {
            return new vec2(a.x / b.x, a.y / b.y);
        }
        //float addition
        public static vec2 operator +(vec2 a, float b)
        {
            return new vec2(a.x + b, a.y + b);
        }
        //float subtraction
        public static vec2 operator -(vec2 a, float b)
        {
            return new vec2(a.x - b, a.y - b);
        }
        //float multiplication
        public static vec2 operator *(vec2 a, float b)
        {
            return new vec2(a.x * b, a.y * b);
        }
        //float division
        public static vec2 operator /(vec2 a, float b)
        {
            return new vec2(a.x / b, a.y / b);
        }
        public override string ToString()
        {
            return x + " " + y;
        }
    }

    public struct Color
    {
        private int ir, ig, ib, ia;
        public int R { get { return ir; } set { ir = value.limit(0,255); }  }
        public int G { get { return ig; } set { ig = value.limit(0, 255); } }
        public int B { get { return ib; } set { ib = value.limit(0, 255); } }
        public int A { get { return ia; } set { ia = value.limit(!RainbowMode ? -255 : 0, 255); } } //-1.0 for alpha triggers the rainbow in the backend
        private static Random rnd = new Random();
        private bool RainbowMode;

        /// <summary>creates a new color from RGB and Alpha values </summary>
        public Color(int r, int g, int b, int a = 255) : this()
        {
            R = r;
            G = g;
            B = b;
            A = a;
            RainbowMode = false;
        }
        /// <summary> creates a monochrome color </summary>
        public Color(int rgb = 255, int a = 255) : this()
        {
            R = rgb;
            G = rgb;
            B = rgb;
            A = a;
            RainbowMode = false;
        }

        public static Color White { get { return new Color(255, 255, 255); } }
        public static Color Black { get { return new Color(0, 0, 0); } }
        public static Color Gray { get { return new Color(100, 100, 100); } }
        public static Color LightGray { get { return new Color(160, 160, 160); } }
        public static Color DarkGray { get { return new Color(70, 70, 70); } }
        public static Color Blue { get { return new Color(50, 200, 255); } }
        public static Color LightBlue { get { return new Color(70, 70, 255); } }
        public static Color DarkBlue { get { return new Color(0, 0, 160); } }
        public static Color Red { get { return new Color(255, 0, 0); } }
        public static Color DarkRed { get { return new Color(150, 0, 0); } }
        public static Color Yellow { get { return new Color(255, 255, 0); } }
        public static Color Orange { get { return new Color(255, 130, 0); } }
        public static Color Purple { get { return new Color(188, 11, 129); } }
        public static Color Pink { get { return new Color(255, 20, 153); } }
        public static Color Green { get { return new Color(0, 255, 0); } }
        public static Color LightGreen { get { return new Color(0, 255, 0); } }
        public static Color DarkGreen { get { return new Color(0, 130, 0); } }
        public static Color Invisible { get { return new Color(0, 0, 0, 0); } }
        public static Color Rainbow
        {
            get
            {
                Color c = new Color(255, 255, 255);
                c.RainbowMode = true;
                return c;
            }
        }
        /// <summary> A random opaque color </summary>
        public static Color Random { get => new Color(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255)); }

        //just in case you're used to system.drawing
        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }
        /// <summary>Averages the rgb values together</summary>
        public void SetMonochrome()
        {
            this = new Color((R + G + B)/3, A);
        }
        public void Invert()
        {
            R = 255 - R;
            G = 255 - G;
            B = 255 - B;
        }

        //implicit color to drawing color
        public static implicit operator System.Drawing.Color(Color c)
        {
            return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        //implicit drawing color to color
        public static implicit operator Color(System.Drawing.Color c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }

        //implicit color to Gl3DrawerCLR RGBA convertion which is implcitly converted to glm vec4 internally
        public static implicit operator GL3DrawerCLR.RGBA (Color c)
        {
            return new GL3DrawerCLR.RGBA(c.R / 255f, c.G / 255f, c.B/255f, c.RainbowMode ? -1f : c.A/255f);
        }
        public static implicit operator Color(GL3DrawerCLR.RGBA c)
        {
            return new Color((int)(c.r * 255), (int)(c.g * 255), (int)(c.b * 255), (int)(c.a * 255));
        }

        public override bool Equals(Object obj)
        {
            return obj is vec2 && this == (Color)obj;
        }

        public override int GetHashCode()
        {
            return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
        }

        public static bool operator ==(Color x, Color y)
        {
            return (x.R == y.R && x.G == y.G && x.B == y.B && x.A == y.A && x.RainbowMode == y.RainbowMode);
        }

        public static bool operator !=(Color x, Color y)
        {
            return !(x == y); 
        }
        public static Color operator +(Color x, Color y)
        {
            return new Color(x.R + y.R, x.G + y.G, x.B + y.B, x.A + y.A);
        }
        public static Color operator -(Color x, Color y)
        {
            return new Color(x.R - y.R, x.G - y.G, x.B - y.B, x.A - y.A);
        }
        public static Color operator *(Color x, Color y)
        {
            return new Color(x.R * y.R, x.G * y.G, x.B * y.B, x.A * y.A);
        }
        public static Color operator /(Color x, Color y)
        {
            return new Color(x.R / y.R, x.G / y.G, x.B / y.B,x.A / y.A);
        }
        public static Color operator +(Color x, float y)
        {
            int val = (int)y * 255;
            return new Color(x.R + val, x.G + val, x.B + val, x.A + val);
        }
        public static Color operator -(Color x, float y)
        {
            int val = (int)y * 255;
            return new Color(x.R - val, x.G - val, x.B - val, x.A - val);
        }
        public static Color operator *(Color x, float y)
        {
            int val = (int)y * 255;
            return new Color(x.R * val, x.G * val, x.B * val, x.A * val);
        }
        public static Color operator /(Color x, float y)
        {
            int val = (int)y * 255;
            return new Color(x.R / val, x.G / val, x.B / val, x.A / val);
        }
        public static Color operator +(Color x, int y)
        {
            int val = y.limit(-255, 255);
            return new Color(x.R + val, x.G + val, x.B + val);
        }
        public static Color operator -(Color x, int y)
        {
            int val = y.limit(-255, 255);
            return new Color(x.R - val, x.G - val, x.B - val);
        }
        public static Color operator *(Color x, int y)
        {
            int val = y.limit(-255, 255);
            return new Color(x.R * val, x.G * val, x.B * val);
        }
        public static Color operator /(Color x, int y)
        {
            int val = y.limit(-255, 255);
            return new Color(x.R / val, x.G / val, x.B / val);
        }
    }
    //might be confusing
    public static partial class ExtentionMethods
    {
        /// <summary>
        /// Limits a float between two values
        /// </summary>
        public static float limit(this float value, float min, float max)
        {
            value = value < min ? min : value;
            return value > max ? max : value;
        }
        /// <summary>
        /// Limits an int between two values
        /// </summary>
        public static int limit(this int value, int min, int max)
        {
            value = value < min ? min : value;
            return value > max ? max : value;
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

