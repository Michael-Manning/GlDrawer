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
        public int R { get => ir; set => ir = limit(value, 0, 255); }
        public int G { get => ig; set => ig = limit(value, 0, 255); }
        public int B { get => ib; set => ib = limit(value, 0, 255); }
        public int A { get => ia; set => ia = limit(value, 0, 255); }
     
        private static Random rnd = new Random();// legacy (slow)
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
            rgb = limit(rgb, 0, 255);
            ir = rgb;
            ig = rgb;
            ib = rgb;
            ia = a;
            RainbowMode = false;
            HazardMode = false;
        }

        public static Color White => new Color(255, 255, 255); 
        public static Color Black => new Color(0, 0, 0); 
        public static Color Gray => new Color(100, 100, 100); 
        public static Color LightGray => new Color(160, 160, 160); 
        public static Color DarkGray => new Color(70, 70, 70);
        public static Color Blue => new Color(50, 200, 255); 
        public static Color LightBlue => new Color(70, 70, 255); 
        public static Color DarkBlue => new Color(0, 0, 160); 
        public static Color Red => new Color(255, 0, 0); 
        public static Color DarkRed => new Color(150, 0, 0); 
        public static Color Yellow => new Color(255, 255, 0); 
        public static Color Orange => new Color(255, 130, 0); 
        public static Color Purple => new Color(188, 11, 129); 
        public static Color Pink => new Color(255, 20, 153); 
        public static Color Green => new Color(0, 255, 0); 
        public static Color LightGreen => new Color(0, 255, 0);
        public static Color DarkGreen => new Color(0, 130, 0); 
        public static Color Cyan => new Color(0, 255, 255); 
        public static Color GreenYellow => new Color(173, 255, 47); 
        public static Color Tomato => new Color(255, 99, 71); 
        public static Color Wheat => new Color(254, 222, 179); 
        public static Color LightCoral => new Color(94, 50, 50); 
        public static Color Invisible => new Color(0, 0, 0, 0); 
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

        //fast color that skips sanity check
        private Color(byte r, byte g, byte b, byte a) : this()
        {
            ir = r;
            ig = g;
            ib = b;
            ia = a;
            RainbowMode = false;
            HazardMode = false;
        }


        private static uint x = 1, y = 2, z = 3, w = 4;
        /// <summary> A random opaque color </summary>
        public static Color Random {
            get
            {
                Color c;
                uint t = x ^ (x << 11);
                x = y; y = z; z = w;
                w = w ^ (w >> 19) ^ (t ^ (t >> 8));
                c.ir = (byte)(w & 0xFF);
                c.ig = (byte)((w >> 8) & 0xFF);
                c.ib = (byte)((w >> 16) & 0xFF);
                c.ia = 255;
                c.RainbowMode = false;
                c.HazardMode = false;
                return c;
            }
        }

        //just in case you're used to system.drawing
        public static Color FromArgb(int a, int r, int g, int b) => new Color(r, g, b, a);
        public static Color FromRed(int r) => new Color(r, 0, 0, 255);
        public static Color FromGreen(int g) => new Color(0, g, 0, 255);
        public static Color FromBlue(int b) => new Color(0, 0, b, 255);

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
        public static void setRandomSeed(int seed) => rnd = new Random(seed);

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
            return new GLDrawerCLR.unmanaged_color(c.ir / 255f, c.ig / 255f, c.ib / 255f, c.RainbowMode ? -1f : c.HazardMode ? -2f : c.ia / 255f);
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
            return (x.ir == y.ir && x.ig == y.ig && x.ib == y.ib && x.A == y.ia && x.RainbowMode == y.RainbowMode);
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
            int val = limit(y, -255, 255);
            return new Color(x.R + val, x.G + val, x.B + val);
        }
        public static Color operator -(Color x, int y)
        {
            int val = limit(y, -255, 255);
            return new Color(x.R - val, x.G - val, x.B - val);
        }
        public static Color operator *(Color x, int y)
        {
            int val = limit(y, -255, 255);
            return new Color(x.R * val, x.G * val, x.B * val);
        }
        public static Color operator /(Color x, int y)
        {
            int val = limit(y, -255, 255);
            return new Color(x.R / val, x.G / val, x.B / val);
        }

        private static int limit(int value, int min, int max)
        {
            value = value < min ? min : value;
            return value > max ? max : value;
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

    }
}
