using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GL3DrawerCLR;

namespace communication
{
    public static class util
    {
        public static void linkCanvas(GLDWrapper GLDW, GLDrawer.Shape shape)
        {

        }
    }
}

namespace GLDrawer
{
    public abstract partial class Shape
    { 
        public vec2 Position
        {
            get { return rect.Pos; }
            set { rect.Pos = value; }
        }
        public vec2 Scale
        {
            get { return rect.Scale; }
            set { rect.Scale = value; }
        }
        public float Angle
        {
            get { return rect.Angle; }
            set { rect.Angle = value; }
        }

        public Color FillColor
        {
            get { return rect.Color; }
            set { rect.Color = value; }
        }
        public Color BorderColor
        {
            get { return rect.BorderColor; }
            set { rect.BorderColor = value; }
        }

        public float BorderWidth
        {
            get { return rect.BordWidth; }
            set { rect.BordWidth = value; }
        }
        public float RotationSpeed
        {
            get { return rect.rSpeed; }
            set { rect.rSpeed = value; }
        }
        public bool Hidden
        {
            get { return rect.hidden; }
            set { rect.hidden = value; }
        }

        public int internalIndex { get { return rect.index; } }

        protected int sides;

        public Rect rect;
       
        protected Color checkNullC(Color? c)
        {
            return c == null ? Color.Invisible : (Color)c;
        }
    }

    public class Rectangle : Shape
    {
        public Rectangle(vec2 position, vec2 scale, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {
            
            rect = new Rect(position, scale, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        ~Rectangle()
        {
            rect.dispose();
        }
    }
    public class Ellipse : Shape
    {
        public Ellipse(vec2 position, vec2 scale, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {
            rect = new Rect(position, scale, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed, 1);
        }
        ~Ellipse()
        {
            rect.dispose();
        }
    }
    public class Line : Shape
    {
        public Line(vec2 position, vec2 scale, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {

            rect = new Rect(position, scale, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        public Line(vec2 start, vec2 end, float width, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {

         //   rect = new Rect(position, scale, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        ~Line()
        {
            rect.dispose();
        }
    }
    public class Text : Shape
    {
        public Text()
        {

            //rect = new Rect(position, scale, angle, checkNullC(fillColor), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        ~Text()
        {
            rect.dispose();
        }
    }
    public class Sprite : Shape
    {
        public string FilePath { get; private set; }
        public Sprite(string filePath, vec2 position, vec2 scale, Color? Tint = null, float borderWidth = 0, Color? borderColor = null, float angle = 0, float rotationSpeed = 0)
        {
            FilePath = filePath;
            rect = new Rect(filePath, position, scale, angle, checkNullC(Tint), checkNullC(borderColor), borderWidth, rotationSpeed);
        }
        ~Sprite()
        {
            rect.dispose();
        }
    }
}
