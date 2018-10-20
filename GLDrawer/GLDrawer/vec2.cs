using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;

namespace GLDrawer
{
#pragma warning disable IDE1006 // Naming Styles
    public struct vec2
#pragma warning restore IDE1006 // Naming Styles
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
        //public static implicit operator GLDrawerCLR.vec2(vec2 v)
        //{
        //    return new GLDrawerCLR.vec2(v.x, v.y);
        //}

        /// <summary>gets the distance from the target</summary>
        public float Length(vec2 Target)
        {
            float a = x - Target.x;
            float b = y - Target.y;
            return (float)Math.Sqrt(a * a + b * b);
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
        public static vec2 Lerp(vec2 current, vec2 target, float time)
        {
            float retX = current.x * time + target.x * (1 - time);
            float retY = current.y * time + target.y * (1 - time);
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
            return x + ", " + y;
        }
    }
}
