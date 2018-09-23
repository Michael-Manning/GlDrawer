using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;

namespace GLDrawer
{
    public enum JustificationType { Left, Center, Right };

    public abstract partial class Shape
    {
        /// <summary>
        /// this simple class primarly interacts with the C++ backend.
        /// An "unmanaged_shape" is a backend structure which contains %90 of the actual shape data and functions.
        /// All shapes are contained in an unmanaged_shape and are simply rendered with different shaders depending 
        /// on the number of sides (-1 = font, 0 = texture, 1 = Ellipse, 4 = Quad, all other positive numbers = polygon).
        /// Text and Sprite contain the only supplimentary data which are their filepaths and the text to be rasterized.
        /// See GLDrawerCLR.h, and engine.h for more details
        /// </summary>
        public vec2 Position { get => new vec2(internalShape.Pos.x, internalShape.Pos.y); set => internalShape.Pos = new GLDrawerCLR.vec2(value.x, value.y); }
        public vec2 Scale { get => new vec2(internalShape.Scale.x, internalShape.Scale.y); set => internalShape.Scale = new GLDrawerCLR.vec2(value.x, value.y); }
        public virtual float Angle { get => internalShape.Angle; set => internalShape.Angle = value; }
        public Color FillColor { get => internalShape.Color; set => internalShape.Color = value; }
        public Color BorderColor { get => internalShape.BorderColor; set => internalShape.BorderColor = value; }
        public float BorderWidth { get => internalShape.BordWidth; set => internalShape.BordWidth = value; }
        public float RotationSpeed { get => internalShape.rSpeed; set => internalShape.rSpeed = value; }
        public bool Hidden { get => internalShape.hidden; set => internalShape.hidden = value; }

        //where the action actually happens
        internal unmanaged_shape internalShape;

        //shortcut to replace unfilled automatic arguements with an empty color
        protected Color checkNullC(Color? c)
        {
            return c == null ? Color.Invisible : (Color)c;
        }
    }

    public class Rectangle : Shape
    {
        public Rectangle(vec2 position, vec2 scale, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {

            internalShape = new unmanaged_shape(position.x, position.y, scale.x, scale.y, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        ~Rectangle()
        {
            internalShape.dispose();
        }
    }
    public class Ellipse : Shape
    {
        public Ellipse(vec2 position, vec2 scale, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {
            internalShape = new unmanaged_shape(position.x, position.y, scale.x, scale.y, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed, 1);
        }
        ~Ellipse()
        {
            internalShape.dispose();
        }
    }
    public class Line : Shape
    {
        public float Thickness
        {
            get { return Scale.y; }
            set
            {
                Scale = new vec2(Scale.x, value);
                recalculate();
            }
        }
        public float Length
        {
            get { return Scale.y; }
            set
            {
                Scale = new vec2(value, Scale.y);
                recalculate();
            }
        }
        private vec2 istart, iend;
        public vec2 Start
        {
            get { return istart; }
            set
            {
                istart = value;
                recalculate();
            }
        }
        public vec2 End
        {
            get { return iend; }
            set
            {
                iend = value;
                recalculate();
            }
        }
        //While the angle is calculated given a start aand endpoint, The oposite must be possible in case the start/end points are read from after changing the angle
        public override float Angle
        {
            get => base.Angle;
            set
            {
                internalShape.Angle = value;
                //UNTESTED!
                istart = Position + new vec2((float)Math.Cos(- value) * (Length / 2f), (float)Math.Sin(- value) * (Length/2f));
                iend = Position + new vec2((float)Math.Cos(value) * (Length / 2f), (float)Math.Sin(value) * (Length / 2f));
            }
        }

        //since a line just wraps around a backend rectangle, some math is required to transform the internalShape given start and endpoints
        private void recalculate()
        {
            float a = (Start.y - End.y);
            float o = (Start.x - End.x);
            internalShape.Angle = (float)Math.Atan(a / o);
            internalShape.Pos = new vec2((Start.x + End.x) / 2, (Start.y + End.y) / 2);
            internalShape.Scale = new vec2((float)Math.Sqrt(a * a + o * o), Thickness);
        }

        //simply defines a internalShape without a transform and calculates the transform once the properties aare triggered
        public Line(vec2 start, vec2 end, float thickness, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float rotationSpeed = 0)
        {
            //position, scale, and angle are calulated afterwards
            internalShape = new unmanaged_shape(0,0,0,0, 0f, checkNullC(fillColor), checkNullC(borderColor), borderWidth, 0f); 
            Start = start;
            End = end;
            Thickness = thickness;
        }

        //UNTESTED!
        public Line(vec2 StartPosition, float length, float thickness, float angle, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float rotationSpeed = 0)
        {
            internalShape = new unmanaged_shape(0, 0, 0, 0, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, 0f);
            Start = StartPosition;
            End = StartPosition + new vec2((float)Math.Cos(angle) * length, (float)Math.Sin(angle) * length);
            Thickness = thickness;
        }

        ~Line()
        {
            internalShape.dispose();
        }
    }
    public class Polygon : Shape
    {
        private int isidecount;
        public int SideCount
        {
            get { return isidecount; }
            set
            {
                if (value < 3)
                    throw new ArgumentOutOfRangeException("sideCount", value, "Polygon must have a minimum of 3 sides");
                isidecount = value;
                internalShape.sides = value;
            }
        }
        public Polygon(vec2 position, vec2 scale, int sideCount, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {           
            internalShape = new unmanaged_shape(position.x, position.y, scale.x, scale.y, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed, sideCount);
            SideCount = sideCount;
        }
        ~Polygon()
        {
            internalShape.dispose();
        }
    }
    public class Text : Shape
    {
        public JustificationType Justification { get => (JustificationType)internalShape.justification; set => internalShape.justification = (int)value; }
        public string Body { get => internalShape.text; set => internalShape.text = value; }
        public string Font { get => internalShape.filepath; }
        public Rectangle BoundingRect { get; private set; }

        //with bound
        public Text(string text, float Height, Rectangle FixedBound, Color? color = null, JustificationType justification = JustificationType.Center,  string font = "c:\\windows\\fonts\\times.ttf", float angle = 0, float rotationSpeed = 0)
        {
            if (!System.IO.File.Exists(font))
                throw new ArgumentException("ttf file was not found", "font");

            BoundingRect = FixedBound;
                internalShape = new unmanaged_shape(text,  Height, checkNullC(color), (int)justification, FixedBound.internalShape, font, angle, rotationSpeed);          
        }
        //without bound
        public Text(vec2 position, string text, float Height, Color? color = null, JustificationType justification = JustificationType.Center, string font = "c:\\windows\\fonts\\times.ttf", float angle = 0, float rotationSpeed = 0)
        {
            if (!System.IO.File.Exists(font))
                throw new ArgumentException("ttf file was not found", "font");

            internalShape = new unmanaged_shape(text, position.x, position.y, Height, checkNullC(color), (int)justification, font, angle, rotationSpeed);
        }
        ~Text()
        {
            internalShape.dispose();
        }
    }
    public class Sprite : Shape
    {
        public string FilePath { get; private set; }
        public Sprite(string filePath, vec2 position, vec2 scale, Color? Tint = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {
            FilePath = filePath;
            internalShape = new unmanaged_shape(filePath, position.x, position.y, scale.x, scale.y, angle, checkNullC(Tint), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        ~Sprite()
        {
            internalShape.dispose();
        }
    }
}
