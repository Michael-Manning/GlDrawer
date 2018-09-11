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
    public partial class GLCanvas
    {
        private bool usePackedShaders = true; //set this as true to compile the last packed version of the shaders into the executable/DLL (see shaders.h for more info)
        private float iTime, iDeltaTime, lastTime;

        public long Time { get => Time; }
        public long DeltaTime { get => DeltaTime; }
        public int Width, Height; //make these responsive properties
        public string WindowTitle { get => gldw.title; set => gldw.title = value; }
        public bool InvertedYAxis = false;
        public float Scale = 1;
        public bool ExtraInfo { set => gldw.titleDetails = value; }
        public bool Vsync; //impliment
        public vec2 Centre { get => new vec2(Width / 2, Height / 2); }

        /// <summary>
        /// Forces the back buffer to be a solid color, even if the window is resized
        /// </summary>
        public bool simpleBackBuffer = false;
        public event Action Update = delegate { };

        private GLDWrapper gldw;
        private Stopwatch timer;
        private static List<Action> mainLoops = new List<Action>();
        private static List<GLDWrapper> activeCanvases = new List<GLDWrapper>(); //list for reference only
        private static List<Action> preLoops = new List<Action>();
        private static Thread loopthread;
        private static Thread mainThread;
        private static bool initialised = false;

        private bool NullRemovalFlag = false; //used for thread safe garbage collection

        //Maintains the drawing thread. Both actions are run from the other thread, but "init" is only runs once
        private void loop(Action init, Action mainloop, GLDWrapper can)
        {
            preLoops.Add(init);
            mainLoops.Add(mainloop);
            activeCanvases.Add(can);
            if (!initialised)
            {
                mainThread = Thread.CurrentThread;
                initialised = true;
                loopthread = new Thread(new ThreadStart(delegate
                {
                    while (true)
                    {
                        //if the end of the main program is reached, all the canvas windows should close
                        if (!mainThread.IsAlive)
                            for (int i = 0; i < activeCanvases.Count; i++)
                                activeCanvases[i].shouldClose = true;

                        //when all canvasas are closed, the thread is aborted
                        if (activeCanvases.Count == 0)
                            Thread.CurrentThread.Abort();

                        for (int i = 0; i < preLoops.Count; i++)
                            preLoops[i]();
                        preLoops.Clear();

                        //canvasas that have closed are simply removed from the update list
                        for (int i = 0; i < mainLoops.Count; i++)
                        {

                            if (activeCanvases[i].shouldClose)
                            {
                                mainLoops.RemoveAt(i);
                                activeCanvases.RemoveAt(i);
                                continue;
                            }
                            mainLoops[i]();
                        }
                    }
                        
                }));
                loopthread.Start();
            }    
        }
        /// <summary>
        /// Creates a new window with a GLCanvas
        /// </summary>
        /// <param name="width">Width of the window</param>
        /// <param name="height">Height of the window</param>
        /// <param name="title">Name of the window title</param>
        /// <param name="BackColor">Background color of the canvas</param>
        /// <param name="borderless">Wether or not the window is borderless</param>
        /// <param name="TitleDetails">Displays render time, FPS, and shape count in the title</param>
        /// <param name="VSync">Limits the framerate to 60fps and waits for vertical screen synchronization</param>
        /// <param name="debugMode">Display rendering information on top of the canvas</param>
        public GLCanvas(int width = 800, int height = 600, string title = "Canvas Window", Color? BackColor = null, bool TitleDetails = false, bool borderless = false, bool VSync = true, bool debugMode = false)
        {
            Width = width;
            Height = height;
            gldw = new GLDWrapper(usePackedShaders);
            gldw.title = title;
            gldw.titleDetails = TitleDetails;
           loop(delegate
           {
               gldw.createCanvas(Width, Height, borderless, BackColor == null ? Color.Black : (Color)BackColor, VSync, debugMode);
               setInputCallbacks();
           },
           delegate 
           {
               mainLoop();
           }, gldw);
        }

        //allows the native HWND process from a GLFW window to be embedded into a windows forms parnel
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        IntPtr panelHandle;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="form">The Windows Form to embed the canvas window</param>
        /// <param name="panel">the panel to replace with the canvas</param>
        /// <param name="BackColor">Background color of the canvas</param>
        /// <param name="VSync">Limits the framerate to 60fps and waits for vertical screen synchronization</param>
        /// <param name="debugMode">Display rendering information on top of the canvas</param>
        public GLCanvas(Form form, Panel panel, Color? BackColor = null, bool VSync = true, bool debugMode = false)
        {        
            Width = panel.Width;
            Height = panel.Height;
            gldw = new GLDWrapper(usePackedShaders);
            form.FormClosed += delegate { Close(); }; //close the canvas when the parent form is closed
            panelHandle = panel.Handle;
            
            loop(delegate
           {
               gldw.createCanvas(Width, Height,true, BackColor == null ? Color.LightGray : (Color)BackColor, VSync, debugMode);
               setInputCallbacks();

               SetParent(gldw.getNativeHWND(), panelHandle);
           },
           delegate 
           {
               mainLoop();
           }, gldw);

        }
        //C++ backend needs to know where to trigger input events
        private void setInputCallbacks()
        {
            gldw.Input.setMouseCallback(MouseCallback);
            gldw.Input.setKeyCallback(KeyCallback);
            gldw.Input.setMouseMoveCallback(MouseMoveCallback);
            timer = new Stopwatch();
            timer.Start();
        }
        private void mainLoop()
        {   
            iTime = timer.ElapsedMilliseconds;
            iDeltaTime = iTime - lastTime;
            lastTime = iTime;

            Update.Invoke();

            if (NullRemovalFlag)
            {
                gldw.cleaarNullRects();
                GC.Collect();
                NullRemovalFlag = false;
            }

            if (simpleBackBuffer)
                gldw.clearBB();
            gldw.mainloop();
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
        public Rectangle AddRectangle(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
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
        public Rectangle AddCenteredRectangle(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
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
        public Ellipse AddEllipse(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
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
        public Ellipse AddCenteredEllipse(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
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
        public Line AddLine(float XStart, float YStart, float XEnd, float YEnd, float Thickness, Color? LineColor = null, float BorderThickness = 0, Color? BorderColor = null, float RotationSpeed = 0)
        {
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
        public Line AddLine(vec2 StartPos, float Length, float Angle, float Thickness, Color? LineColor = null, float BorderThickness = 0, Color? BorderColor = null, float RotationSpeed = 0)
        {
            Line l = new Line(StartPos, Length, Thickness, Angle, LineColor, BorderThickness, BorderColor, RotationSpeed);
            gldw.addRect(l.rect);
            return l;
        }
        public Polygon AddCenteredPolygon(float Xpos, float Ypos, float Width, float Height, int SideCount, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
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
            Sprite s = new Sprite(FilePath, new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            gldw.addRect(s.rect);
            return s;
        }
        public Shape Add(Shape shape)
        {
            gldw.addRect(shape.rect);
            return shape;
        }
        public void RemoveShape(Shape s)
        {
            gldw.removeRect(s.rect);
        }
        public void SendBack(Shape s)
        {

        }
        public void SendForward(Shape s)
        {

        }
        public void SendToBack(Shape s)
        {

        }
        public void SendToFront(Shape s)
        {

        }
        public void SwapDrawOrder(int IndexA, int IndexB)
        {
            gldw.swapOrder(IndexA, IndexB);
        }
        public void SetBBPixel(int x, int y, Color color)
        {
            if (x < 0 || x > Width) 
                throw new ArgumentException("X coordinate must be a positive number less than the canvas width", "x");
            if (y < 0 || y > Height)
                throw new ArgumentException("Y coordinate must be a positive number less than the canvas height", "y");
            gldw.setBBpixel(x, y, color);
        }
        public Shape SetBBShape(Shape s)
        {
            return s;
        }
        public Color getPixel(int x, int y) => gldw.getPixel(x, y);
        public Color getPixel(vec2 pixel) => gldw.getPixel((int)pixel.x, (int)pixel.y);

        //when a refrence to an added shape is deleted, it may not stop drawing until this is called
        public void Refresh() => NullRemovalFlag = true;
        //{
        //    gldw.cleaarNullRects();
        //    GC.Collect();
        //}
        public void Close()
        {
            gldw.shouldClose = true;
            //gldw.dispose();
        }
        public Color BackBufferColor { get => gldw.backColor; set => gldw.backColor = value; }
        public void ClearBackBuffer() => gldw.clearBB();
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
        public static vec2 Max { get { return new vec2(float.MaxValue); } }
        public vec2 Abs { get { return new vec2(Math.Abs(x), Math.Abs(y)); } }

        public GL3DrawerCLR.vec2 ImplicitConversion
        {
            get => default(GL3DrawerCLR.vec2);
            set
            {
            }
        }

        //implicit vec2 to Gl3DrawerCLR vec2 convertion which is implcitly converted to glm vec2 internally
        public static implicit operator GL3DrawerCLR.vec2(vec2 v)
        {
            return new GL3DrawerCLR.vec2(v.x, v.y);
        }
        //public static implicit operator vec2(GL3DrawerCLR.vec2 v)
        //{
        //    return new vec2(v.x, v.y);
        //}

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
        public vec2 Normalize()
        {
            float distance = (float)Math.Sqrt(this.x * this.x + this.y * this.y);
            return new vec2(this.x / distance, this.y / distance);
        }
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

        /// <summary>
        /// creates a new color from RGB and Alpha values
        /// </summary>
        public Color(int r, int g, int b, int a = 255) : this()
        {
            R = r;
            G = g;
            B = b;
            A = a;
            RainbowMode = false;
        }
        /// <summary>
        /// creates a monochrome color 
        /// </summary>
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
        /// <summary>
        /// A random opaque color
        /// </summary>
        public static Color Random { get => new Color(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255)); }

        //just in case you're used to system.drawing
        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }
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

