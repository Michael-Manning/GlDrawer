using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;

namespace GLDrawer
{
    public struct Color
    {
        private int ir, ig, ib, ia;
        public int R { get => ir; set => ir = value.limit(0, 255); }
        public int G { get => ig; set => ig = value.limit(0, 255); }
        public int B { get => ib; set => ib = value.limit(0, 255); }
        public int A { get => ia; set => ia = value.limit(0, 255); }
     
        private static Random rnd = new Random();
        private bool RainbowMode, HazardMode;

        /// <summary>creates a new color from RGB and Alpha values </summary>
        public Color(int r, int g, int b, int a = 255) : this()
        {
            R = r;
            G = g;
            B = b;
            A = a;
            RainbowMode = false;
            HazardMode = false;
        }
        /// <summary> creates a monochrome color </summary>
        public Color(int rgb = 255, int a = 255) : this()
        {
            R = rgb;
            G = rgb;
            B = rgb;
            A = a;
            RainbowMode = false;
            HazardMode = false;
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
        public static Color Hazard
        {
            get
            {
                Color c = new Color(255, 255, 255);
                c.HazardMode = true;
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
            this = new Color((R + G + B) / 3, A);
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

        //implicit color to GLDrawerCLR unmanaged_color convertion which is implcitly converted to glm vec4 internally
        public static implicit operator GLDrawerCLR.unmanaged_color(Color c)
        {
            return new GLDrawerCLR.unmanaged_color(c.R / 255f, c.G / 255f, c.B / 255f, c.RainbowMode ? -1f : c.HazardMode ? -2f : c.A / 255f);
        }
        public static implicit operator Color(GLDrawerCLR.unmanaged_color c)
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
            return new Color(x.R / y.R, x.G / y.G, x.B / y.B, x.A / y.A);
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
}
