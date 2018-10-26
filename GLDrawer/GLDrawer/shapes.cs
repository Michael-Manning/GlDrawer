using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;

namespace GLDrawer
{
    public enum JustificationType { Left, Center, Right };

    public abstract partial class Shape : IDisposable
    {
        /// this simple class primarly interacts with the C++ backend.
        /// An "unmanaged_shape" is a backend structure which contains %90 of the actual shape data and functions.
        /// All shapes are contained in an unmanaged_shape and are simply rendered with different shaders depending 
        /// on the number of sides (-1 = font, 0 = texture, 1 = Ellipse, 4 = Quad, all other positive numbers = polygon).
        /// Text and Sprite contain the only supplimentary data which are their filepaths and the text to be rasterized.
        /// See GLDrawerCLR.h, and engine.h for more details

        public vec2 Position { get => new vec2(internalGO.position.x, internalGO.position.y); set => internalGO.position = new unmanaged_vec2(value.x, value.y); }
        public vec2 Scale { get => new vec2(internalGO.scale.x, internalGO.scale.y); set => internalGO.scale = new unmanaged_vec2(value.x, value.y); }
        public virtual float Angle { get => internalGO.angle; set => internalGO.angle = value; }

        public float RotationSpeed { get => internalGO.rSpeed; set => internalGO.rSpeed = value; }
        public bool Hidden { get => internalGO.hidden; set => internalGO.hidden = value; }
        public int DrawIndex { get => internalGO.drawIndex; set => internalGO.drawIndex = value; }
        
        /// <summary>
        /// returns true if the point is inside the shape
        /// </summary>
        /// <param name="point"> location to test</param>
        /// <returns></returns>
        public virtual bool Intersect(vec2 point)
        {
            return unmanaged_Canvas.TestRect(Position.x, Position.y, Scale.x, Scale.y, Angle, point.x, point.y);
        }

        //used only with gameobjects internally
        private GameObject iparent;
        internal GameObject Parent
        {
            get => iparent;
            set
            {
                internalGO.setParent(value.internalGO);
                iparent = value;
            }
        }
        internal bool isCircle = false; //saves hassel with physics

        public void ClearParent()
        {
            internalGO.clearParent();
            iparent = null;
        }

        //where the action actually happens
        internal unmanaged_GO internalGO;

        //shortcut to replace unfilled automatic arguements with an empty color
        protected Color CheckNullC(Color? c) 
        {
            return c == null ? Color.Invisible : (Color)c;
        }

        protected bool disposed = false;
        public abstract void Dispose();
    }

    public class Line : Shape 
    {
        internal unmanaged_polyData internalPoly;
        public Color FillColor { get => internalPoly.fColor; set => internalPoly.fColor = value; }
        public Color BorderColor { get => internalPoly.bColor; set => internalPoly.bColor = value; }
        public float BorderWidth { get => internalPoly.bWidth; set => internalPoly.bWidth = value; }
          
        public float Thickness
        {
            get { return Scale.y; }
            set
            {
                Scale = new vec2(Scale.x, value);
                Recalculate();
            }
        }
        public float Length
        {
            get { return Scale.y; }
            set
            {
                Scale = new vec2(value, Scale.y);
                Recalculate();
            }
        }
        private vec2 istart, iend;
        public vec2 Start
        {
            get { return istart; }
            set
            {
                istart = value;
                Recalculate();
            }
        }
        public vec2 End
        {
            get { return iend; }
            set
            {
                iend = value;
                Recalculate();
            }
        }
        //While the angle is calculated given a start aand endpoint, The oposite must be possible in case the start/end points are read from after changing the angle
        public override float Angle
        {
            get => base.Angle;
            set
            {
                internalGO.angle = value;
                //UNTESTED!
                istart = Position + new vec2((float)Math.Cos(- value) * (Length / 2f), (float)Math.Sin(- value) * (Length/2f));
                iend = Position + new vec2((float)Math.Cos(value) * (Length / 2f), (float)Math.Sin(value) * (Length / 2f));
            }
        }

        //since a line just wraps around a backend rectangle, some math is required to transform the internalShape given start and endpoints
        private void Recalculate()
        {
            float a = (Start.y - End.y);
            float o = (Start.x - End.x);
            internalGO.angle = (float)Math.Atan(a / o);
            internalGO.position = new unmanaged_vec2((Start.x + End.x) / 2, (Start.y + End.y) / 2);
            internalGO.scale = new unmanaged_vec2((float)Math.Sqrt(a * a + o * o), Thickness);
        }

        //simply defines a internalShape without a transform and calculates the transform once the properties aare triggered
        public Line(vec2 start, vec2 end, float thickness, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float rotationSpeed = 0)
        {
            //position, scale, and angle are calulated afterwards
            internalPoly = new unmanaged_polyData(CheckNullC(fillColor), CheckNullC(borderColor), borderWidth, 4);
            internalGO = new unmanaged_GO(internalPoly, 0, 0, 0, 0, 0, rotationSpeed);
            Start = start;
            End = end;
            Thickness = thickness;
        }

        public Line(vec2 StartPosition, float length, float thickness, float angle, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float rotationSpeed = 0)
        {
            internalPoly = new unmanaged_polyData(CheckNullC(fillColor), CheckNullC(borderColor), borderWidth, 4);
            internalGO = new unmanaged_GO(internalPoly, 0, 0, 0, 0, 0, rotationSpeed);
            Start = StartPosition;
            End = StartPosition + new vec2((float)Math.Cos(angle) * length, (float)Math.Sin(angle) * length);
            Thickness = thickness;
        }

        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            internalGO.dispose();
            internalPoly.dispose();
            GC.SuppressFinalize(this);
        }

        ~Line()
        {
            Dispose();
        }
    }
    public class Polygon : Shape
    {
        internal unmanaged_polyData internalPoly;
        public Color FillColor { get => internalPoly.fColor; set => internalPoly.fColor = value; }
        public Color BorderColor { get => internalPoly.bColor; set => internalPoly.bColor = value; }
        public float BorderWidth { get => internalPoly.bWidth; set => internalPoly.bWidth = value; }
        private int isidecount;

        public int SideCount
        {
            get { return isidecount; }
            set
            {
                if (value == 2)
                    throw new ArgumentOutOfRangeException("sideCount", value, "Polygon must have a minimum of 3 sides");
                isidecount = value;
                internalPoly.sides = value;
                isCircle = SideCount == 1;
            }
        }
        public Polygon(vec2 position, vec2 scale, float angle = 0, int sideCount = 4, Color? fillColor = null, float borderWidth = 0, Color? borderColor = null, float rotationSpeed = 0)
        {
            internalPoly = new unmanaged_polyData(CheckNullC(fillColor), CheckNullC(borderColor), borderWidth, sideCount);
            internalGO = new unmanaged_GO(internalPoly, position.x, position.y, scale.x, scale.y, angle, rotationSpeed);
            SideCount = sideCount;
            isCircle = SideCount == 1;
        }
        public override bool Intersect(vec2 point)
        {
            if (SideCount == 1)
                return unmanaged_Canvas.TestRect(Position.x, Position.y, Scale.x, Scale.y, Angle, point.x, point.y);
            else
                return base.Intersect(point);
        }
        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            internalGO.dispose();
            internalPoly.dispose();
            GC.SuppressFinalize(this);
        }

        ~Polygon()
        {
            Dispose();
        }
    }
    public class Text : Shape
    {
        internal unmanaged_textData internalText;
        public Color Color { get => internalText.color; set => internalText.color = value; }
        public JustificationType Justification { get => (JustificationType)internalText.justification; set => internalText.justification = (int)value; }
        public string Body { get => internalText.text; set => internalText.text = value; }
        public string Font { get => internalText.filepath; }
        public float Height { get => internalText.height; set => internalText.height = value; }

        //with bound
        public Text(string text, float Height, Color? color = null, JustificationType justification = JustificationType.Center,  string font = "c:\\windows\\fonts\\times.ttf", float angle = 0, float rotationSpeed = 0)
        {
        //    if (!System.IO.File.Exists(font))
                throw new ArgumentException("ttf file was not found", "font");

              //  internalShape = new unmanaged_shape(text,  Height, checkNullC(color), (int)justification, FixedBound.internalShape, font, angle, rotationSpeed);          
        }
        //without bound
        public Text(vec2 position, string text, float Height, Color? color = null, JustificationType justification = JustificationType.Center, string font = "c:\\windows\\fonts\\times.ttf", float angle = 0, float rotationSpeed = 0)
        {
            if (!System.IO.File.Exists(font))
                throw new ArgumentException("ttf file was not found", "font");

            internalText = new unmanaged_textData(text, Height, CheckNullC(color), (int)justification, font, false);
            internalGO = new unmanaged_GO(internalText, position.x, position.y, 0, 0, angle, rotationSpeed);
        }
        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            internalGO.dispose();
            internalText.dispose();
            GC.SuppressFinalize(this);
        }

        ~Text()
        {
            Dispose();
        }
    }
    public class Sprite : Shape
    {
        internal unmanaged_imgData internalImage;
        public string FilePath { get; private set; }
        public float Opacity { get => internalImage.opacity; set => internalImage.opacity = value; }
        public Color Tint { get => internalImage.tint; set => internalImage.tint = value; }
        public vec2 UVOffset { get => new vec2(internalImage.uvPos.x, internalImage.uvPos.y); set => internalImage.uvPos = new unmanaged_vec2(value.x, value.y); }
        public vec2 UVScale { get => new vec2(internalImage.uvScale.x, internalImage.uvPos.y); set => internalImage.uvScale = new unmanaged_vec2(value.x, value.y); }
        public Sprite(string filePath, vec2 position, vec2 scale, float angle = 0, Color? tint = null, vec2? uvScale = null, vec2? uvOffset = null, float rotationSpeed = 0)
        {
            if (!System.IO.File.Exists(filePath))
                throw new ArgumentException("image file was not found", "filepath");

            FilePath = filePath;
            internalImage = new unmanaged_imgData(filePath);
            if (tint != null)
                internalImage.tint = tint;
            internalGO = new unmanaged_GO(internalImage, position.x, position.y, scale.x, scale.y, angle, rotationSpeed);
            if (uvScale != null)
                UVScale = (vec2)uvScale;
            if (uvOffset != null)
                UVOffset = (vec2)uvOffset;
        }
        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            internalGO.dispose();
            internalImage.dispose();
            GC.SuppressFinalize(this);
        }
      //  private vec2 size;
        void SetAnimation(int TilesPerLine, float Duration)
        {
            if (!System.IO.File.Exists(FilePath))
                throw new ArgumentException("image file was not found", "texturePath");
            System.Drawing.Image img = System.Drawing.Image.FromFile(FilePath);
            if (img.Width != img.Height)
                throw new ArgumentException("Image not square");
            internalImage.setAnimation(img.Height, TilesPerLine, Duration);
            img.Dispose();
        }

        ~Sprite()
        {
            Dispose();
        }
    }
}
