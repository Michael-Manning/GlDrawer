using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GL3DrawerCLR;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GLDrawer
{
    public partial class GLCanvas
    {
        public int Width, Height;
        public string WindowTitle;
        public bool InvertedY = false;
        public float scale = 1;

        private GLDWrapper gldw;
        private Thread loopthread;
        private static List<Action> mainLoops = new List<Action>();
        private static List<Action> preLoops = new List<Action>();
        private static Thread loopThread;
        private static bool initialised = false;
        private void loop(Action init, Action mainloop)
        {
            preLoops.Add(init);
            mainLoops.Add(mainloop);
            if (!initialised)
            {
                initialised = true;
                loopthread = new Thread(new ThreadStart(delegate
                {
                    while (true)
                    {
                        for (int i = 0; i < preLoops.Count; i++)
                            preLoops[i]();
                        preLoops.Clear();
                        for (int i = 0; i < mainLoops.Count; i++)
                            mainLoops[i]();
                    }
                        
                }));
                loopthread.Start();
            }    
        }

        public GLCanvas(int width = 800, int height = 600, string title = "GLdrawer ", bool borderless = false)
        {
            Width = width;
            Height = height;
            WindowTitle = title;
            gldw = new GLDWrapper();          
            loopthread = new Thread(new ThreadStart(delegate
            {
                gldw.createCanvas(width, height, borderless, Color.LightGray);
                gldw.Input.setMouseCallback(MouseCallback);
                gldw.Input.setKeyCallback(KeyCallback);
                
                while (true)
                {
           
                    gldw.mainloop();
                }
            }));
            loopthread.Start();
        }
        public GLCanvas(Form form, Panel panel, string title = "GLdrawer ", Color? BackColor = null)
        {
            Width = panel.Width;
            Height = panel.Height;
            WindowTitle = title;
            gldw = new GLDWrapper();
            //get the default window title height from the form to offset the border           
            System.Drawing.Rectangle screenRectangle = form.RectangleToScreen(form.ClientRectangle);
            int titleHeight = screenRectangle.Top - form.Top;
            
            loop(delegate
           {
               gldw.createCanvas(Width, Height,true, BackColor == null ? Color.LightGray : (Color)BackColor);
               setInputCallbacks();
           },
           delegate 
           { 
                   //solve for the x constant
                   gldw.setPos(form.Location.X + panel.Location.X + 8, form.Location.Y + panel.Location.Y + titleHeight);
                   gldw.mainloop();

           });
           // form.Activated += delegate { gldw.focusWindow(); };

            //loopthread = new Thread(new ThreadStart(delegate
            //{     
            //    gldw.createCanvas(Width, Height, true);
            //    gldw.Input.setMouseCallback(MouseCallback);
            //    gldw.Input.setKeyCallback(KeyCallback);

            //    while (true)
            //    {
            //        //solve for the x constant
            //        gldw.setPos(form.Location.X + panel.Location.X + 8, form.Location.Y + panel.Location.Y + titleHeight);
            //        gldw.mainloop();
            //    }
            //}));
            //loopthread.Start();
        }
        void setInputCallbacks()
        {
            gldw.Input.setMouseCallback(MouseCallback);
            gldw.Input.setKeyCallback(KeyCallback);
            gldw.Input.setMouseMoveCallback(MouseMoveCallback);
        }

        private Color checkNullC(Color ? c)
        {
            return c == null ? Color.Invisible : (Color)c;
        }
        public Rectangle AddRectangle(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0)
        {
            Rectangle r = new Rectangle(new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle);
            gldw.addRect(r.rect);
            return r;
        }
        public Rectangle AddCenteredRectangle(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Rectangle r = new Rectangle(new vec2(Xpos, Ypos), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);             
            gldw.addRect(r.rect);
            return r;
        }
        public Ellipse AddEllipse(float XStart, float YStart, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0)
        {
            Ellipse e = new Ellipse(new vec2(XStart + Width / 2f, YStart + Height / 2f), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle);
            gldw.addRect(e.rect);
            return e;
        }
        public Ellipse AddCenteredEllipse(float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Ellipse e = new Ellipse(new vec2(Xpos, Ypos), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            gldw.addRect(e.rect);
            return e;
        }
        public Sprite AddCenteredSprite(string FilePath, float Xpos, float Ypos, float Width, float Height, Color? FillColor = null, float BorderThickness = 0, Color? BorderColor = null, float Angle = 0, float RotationSpeed = 0)
        {
            Sprite s = new Sprite(FilePath, new vec2(Xpos, Ypos), new vec2(Width, Height), FillColor, BorderThickness, BorderColor, Angle, RotationSpeed);
            gldw.addRect(s.rect);
            return s;
        }
        public void RemoveShape(Shape s)
        {
            if(s != null)
                gldw.removeRect(s.rect);
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
        //when a refrence to an added shape is delete, it may not stop drawing until this is called
        public void Refresh()
        {
            GC.Collect();
            gldw.cleaarNullRects();
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
        public static implicit operator vec2(GL3DrawerCLR.vec2 v)
        {
            return new vec2(v.x, v.y);
        }

        public float length(vec2 Target)
        {
            return (float)Math.Sqrt(Math.Pow(x - Target.x, 2) + Math.Pow(y - Target.y, 2));
        }

        public override bool Equals(Object obj)
        {
            return obj is vec2 && this == (vec2)obj;
        }
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
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
        public int A { get { return ia; } set { ia = value.limit(0, 255); } }
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
        public static Color Blue { get { return new Color(0, 0, 255); } }
        public static Color LightBlue { get { return new Color(0, 255, 255); } }
        public static Color DarkBlue { get { return new Color(0, 0, 160); } }
        public static Color Red { get { return new Color(255, 0, 0); } }
        public static Color DarkRed { get { return new Color(150, 0, 0); } }
        public static Color Yellow { get { return new Color(255, 255, 0); } }
        public static Color Orange { get { return new Color(255, 130, 0); } }
        public static Color Purple { get { return new Color(214, 2, 175); } }
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
        //just in case you're used to system.drawing
        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// A random opaque color
        /// </summary>
        public static Color Random
        {
            get
            {
                return new Color(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
            }
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
            return new GL3DrawerCLR.RGBA(c.R / 255f, c.G / 255f, c.B/255f, c.A/255f);
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

        public RGBA ImplicitConversion
        {
            get => default(RGBA);
            set
            {
            }
        }
    }
    public static partial class ExtentionMethods
    {
        public static vec2 lerp(this vec2 first, vec2 second, float time)
        {
            float retX = first.x * time + second.x * (1 - time);
            float retY = first.y * time + second.y * (1 - time);
            return new vec2(retX, retY);
        }
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

