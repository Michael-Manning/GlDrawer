using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor
{
    class Program
    {
        static void Main(string[] args)
        {
        }


        static GLCanvas can;
        static Text text;
        static Polygon page, cursor;
        static bool cursorBlink = false;
        static int initialWidth = 1000, initialHight = 1500, pageMargin = 90, textMargin = 28, textHeight = 20, pageWidth = initialWidth - pageMargin;
        static float cursorTimer = 0;
        static int cursorPosition = 0;
        static float scrollSpeed = 10f, zoomSpeed = 0.01f;

        public static void run()
        {
            can = new GLCanvas(initialWidth, initialHight, BackColor: new Color(200));
            text = can.Add(new Text(vec2.Zero, new vec2(initialWidth - pageMargin - textMargin, initialHight - pageMargin - textMargin), "", textHeight, Color.Black, JustificationType.Left)) as Text;
            text.DrawIndex = 0;
            page = can.AddCenteredRectangle(0, 0, pageWidth, initialHight - pageMargin, Color.White, 4, new Color(50, 14));
            page.DrawIndex = 3;
            cursor = can.Add(new Polygon(vec2.Zero, new vec2(4, textHeight), 0, 4, Color.DarkGray)) as Polygon;
            cursor.DrawIndex = 2;

            can.KeyDown += Can_KeyDown;
            can.KeyUp += Can_KeyUp;
            can.Update += Can_Update;
            can.CanvasResized += Can_CanvasResized;
            can.MouseLeftClick += Can_MouseLeftClick;
            can.MouseScrolled += Can_MouseScrolled;
            Console.ReadKey();
        }

        private static void Can_MouseScrolled(int Delta, GLCanvas Canvas)
        {
            if (can.GetSpecialKey(SpecialKeys.LEFTCONTROL))
            {
                can.CameraZoom += Delta * zoomSpeed;
                if (can.CameraZoom > 1.1f)
                    can.CameraZoom = 1.1f;
                else if (can.CameraZoom < 0.3f)
                    can.CameraZoom = 0.3f;
            }

            else
            {
                can.CameraPosition += new vec2(0, Delta * scrollSpeed);
                if (can.CameraPosition.y > can.Height / 2)
                    can.CameraPosition = new vec2(0, can.Height / 2);
                if (can.CameraPosition.y < -can.Height / 2)
                    can.CameraPosition = new vec2(0, -can.Height / 2);
            }

        }

        //does aproximately 10,000 times as much work as it needs to
        private static void Can_MouseLeftClick(vec2 Position, GLCanvas Canvas)
        {
            if (!text.Intersect(Position) || text.Body.Length == 0)
                return;

            float record = 0;
            int index = -1;
            for (int i = 0; i < text.Body.Length; i++)
            {
                vec2 pos = text.GetLetterPosNDC(i)
                                * new vec2(can.Width, can.Height) / 2
                                + new vec2(-text.Scale.x / 2, text.Scale.y / 2)
                               ;// + new vec2(-textHeight / 4, 0);

                float length = pos.Length(Position);
                if (index == -1 || length < record)
                {
                    record = length;
                    index = i;
                }
            }

            cursorPosition = index == 0 ? index : index + 1;
            resetCursorBlink();
        }

        private static void Can_CanvasResized(int Width, int Height, GLCanvas Canvas)
        {
            text.Scale = new vec2(pageWidth - textMargin, Height - pageMargin - textMargin);
            page.Scale = new vec2(pageWidth, Height - pageMargin);
        }


        static float downDelta = 0;
        static Keys held;
        static bool down = false;
        private static void Can_Update()
        {
            if (cursorPosition == text.Body.Length)
                cursor.Position = text.lastLetterPos + new vec2(textHeight / 4, -textHeight / 4);
            else if (text.Body.Length > 0)
                cursor.Position = text.GetLetterPosNDC(cursorPosition)
                                * new vec2(can.Width, can.Height) / 2
                                + new vec2(-text.Scale.x / 2, text.Scale.y / 2)
                                + new vec2(-textHeight / 4, 0);

            cursorTimer += can.DeltaTime;
            if (cursorTimer > 0.4)
            {
                cursorBlink = !cursorBlink;
                cursor.Hidden = cursorBlink;
                cursorTimer = 0;
            }

            if (!down)
            {
                downDelta = 0;
                return;
            }

            downDelta += can.DeltaTime;
            if (downDelta > 0.4f)
            {
                type(held);
                downDelta = 0.470f;
            }
        }

        static void type(Keys Code)
        {
            held = Code;
            resetCursorBlink();

            char c = can.LastKey;

            if (c >= 32 && c <= 122)
            {
                if ((can.GetSpecialKey(SpecialKeys.LEFTSHIFT) || can.GetSpecialKey(SpecialKeys.RIGHTSHIFT)))
                {
                    switch (c)
                    {
                        case ('1'):
                            c = '!';
                            break;
                        case ('/'):
                            c = '?';
                            break;
                        default:
                            c = char.ToUpper(c);
                            break;
                    }
                }

                else
                    c = char.ToLower(c);
            }


            if (Code == System.Windows.Forms.Keys.Enter)
                c = '\n';
            else if (Code == Keys.Back)
            {
                if (text.Body.Length > 0 && cursorPosition > 0)
                {
                    text.Body = text.Body.Substring(0, cursorPosition - 1) + text.Body.Substring(cursorPosition);
                    cursorPosition--;
                    down = true;
                }
                return;
            }
            else if (Code == Keys.Left)
            {
                if (cursorPosition > 0)
                    cursorPosition--;
                down = true;
                return;
            }
            else if (Code == Keys.Right)
            {
                if (text.Body.Length > cursorPosition)
                    cursorPosition++;
                down = true;
                return;
            }
            if (c < 32 || c > 126 || Code == Keys.LShiftKey || Code == Keys.RShiftKey)
                if (c != '\n')
                    return;

            text.Body = text.Body.Substring(0, cursorPosition) + c + text.Body.Substring(cursorPosition);
            cursorPosition++;
            down = true;
        }

        private static void Can_KeyDown(System.Windows.Forms.Keys Code, GLCanvas Canvas)
        {
            type(Code);
        }
        private static void Can_KeyUp(Keys Code, GLCanvas Canvas)
        {
            down = false;
        }
        static void resetCursorBlink()
        {
            cursorTimer = 0;
            cursor.Hidden = false;
            cursorBlink = false;
        }

    }
}
