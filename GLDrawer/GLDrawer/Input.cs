using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;
using System.Windows.Forms;

namespace GLDrawer
{
    public delegate void GLMouseEvent(vec2 Position, GLCanvas Canvas);

    public partial class GLCanvas
    {
        //most of these events have been included to copy GLDrawer functionality which is why forms keys are being used
        public delegate void GLMouseEvent(vec2 Position, GLCanvas Canvas);
        public delegate void GLScrollEvent(int Delta, GLCanvas Canvas);
        public delegate void GLKeyEvent(Keys Code, GLCanvas Canvas);
        public event GLMouseEvent MouseLeftClick = delegate { };
        public event GLMouseEvent MouseRightClick = delegate { };
        public event GLMouseEvent MouseLeftClickScaled = delegate { };
        public event GLMouseEvent MouseRightClickScaled = delegate { };
        public event GLMouseEvent MouseLeftRelease = delegate { };
        public event GLMouseEvent MouseRightRelease = delegate { };
        public event GLMouseEvent MouseLeftReleaseScaled = delegate { };
        public event GLMouseEvent MouseRightReleaseScaled = delegate { };
        public event GLMouseEvent MouseMove = delegate { };
        public event GLMouseEvent MouseMoveScaled = delegate { };
        public event GLScrollEvent MouseScrolled = delegate { };
        public event GLKeyEvent KeyDown = delegate { };
        public event GLKeyEvent KeyUp = delegate { };

        private KeysConverter kc = new KeysConverter();

        private vec2 iMousePosition = vec2.Zero;
        public vec2 MousePosition { get { return CheckInvert(iMousePosition); } }
        public vec2 MousePositionScaled { get { return CheckInvert(iMousePosition) / Scale; } }
        public vec2 MousePositionWorldSpace { get { return CheckInvert(iMousePosition) - this.Centre; } }
        public bool MouseLeftState { get => !leftLifted; }
        public bool MouseRightState { get => !rightLifted; }
        public int MouseScrollDirection { get; private set; }

        public void KeyCallback(int key, int action, int scancode)
        {
            Keys k = IntToKeys(key);
            //Console.WriteLine(k.ToString() + " " + (action == 1 ? "PRESSED" : "LIFTED"));
            if (key >= (int)SpecialKeys.D0 && key <= (int)SpecialKeys.D9)
                lastNumber = key - (int)SpecialKeys.D0;
            //ignores numlock
            else if (key >= (int)SpecialKeys.NP0 && key <= (int)SpecialKeys.NP9)
                lastNumber = key - (int)SpecialKeys.NP0;
     
            if (action == 1)
                KeyDown.Invoke(k, this);
            if (action == 0)
                KeyUp.Invoke(k, this);
        }

        //this gets called by GLFW (unmanaged code)
        bool leftLifted = true, rightLifted = true;
        private void MouseCallback(int btn, int action, int mods)
        {
            unmanaged_vec2 v = GLWrapper.getMousePos();
            iMousePosition = new vec2(v.x, v.y);
            if(btn == 3)
            {
                MouseScrolled.Invoke(action, this);
                MouseScrollDirection = action;
                return;
            }
            if (btn == 0 && action == 1)
            {
                if (leftLifted)
                {
                    MouseLeftClick.Invoke(MousePosition, this);
                    MouseLeftClickScaled.Invoke(MousePositionScaled, this);
                    iLastLeft = MousePosition;
                    leftClickToggle = true;
                }          
                leftLifted = false;

            }
            if (btn == 0 && action == 0)
            {
                leftLifted = true;
                MouseLeftRelease.Invoke(MousePosition, this);
                MouseLeftReleaseScaled.Invoke(MousePositionScaled, this);
                return;
            }
                

            if (btn == 1 && action == 1)
            {
                if (rightLifted)
                {
                    MouseRightClick.Invoke(MousePosition, this);
                    MouseRightClickScaled.Invoke(MousePositionScaled, this);
                    iLastRight = MousePosition;
                    rightClickToggle = true;
                }
                rightLifted = false;
                return;
            }
            if (btn == 1 && action == 0)
            {
                rightLifted = true;
                MouseRightRelease.Invoke(MousePosition, this);
                MouseRightReleaseScaled.Invoke(MousePositionScaled, this);
                return;
            }
        }
        private void MouseMoveCallback()
        {
            unmanaged_vec2 v = GLWrapper.getMousePos();
            iMousePosition = new vec2(v.x, v.y);
            MouseMove.Invoke(MousePosition, this);
            MouseMoveScaled.Invoke(MousePositionScaled, this);
        }
        private vec2 CheckInvert(vec2 v)
        {
            if(!InvertedYAxis && !BottomLeftZero)
                return new vec2(v.x - Width/2, -v.y + Height /2);
            if (!InvertedYAxis)
                return new vec2(v.x, -v.y + Height);
            return v;
        }

        #region get key functions
        /// <summary>
        /// Returns true if the key is currently held down
        /// <param name="key">Any letter or ASCII supported key</param>
        /// </summary>
        public bool GetKey(char key)
        {
            if (key > 128 || key < 0)
                throw new ArgumentException("Key out of supported ASCII range (0-127)");
            return GLWrapper.getKey(char.ToUpper(key));
        }
        /// <summary>
        /// Returns true if the letter key was was pressed down during the current frame
        /// <param name="key">Letter or ASCII key to check if pressed</param>
        /// </summary>
        public bool GetKeyDown(char key)
        {
            if (key > 128 || key < 0)
                throw new ArgumentException("Key out of supported ASCII range (0-127)");
            return GLWrapper.getKeyDown(char.ToUpper(key));
        }
        /// <summary>
        /// Returns true if the letter key was was lifted up during the current frame
        /// <param name="key">Letter or ASCII key to check if lifted. also accepts ASCII codes such as backspace</param>
        /// </summary>
        public bool GetKeyUp(char key)
        {
            if (key > 128 || key < 0)
                throw new ArgumentException("Key out of supported ASCII range (0-127)");
            return GLWrapper.getKeyUp(char.ToUpper(key));
        }
        /// <summary>
        /// Returns true if the key is being held down
        /// <param name="button">keycode to check if pressed</param>
        /// </summary>
        public bool GetSpecialKey(SpecialKeys button)
        {
            return GLWrapper.getKey((int)button);
        }
        /// <summary>
        /// Returns true if the key was was pressed during tthe current frame
        /// <param name="button">keycode to check if pressed</param>>
        /// </summary>
        public bool GetSpecialKeyDown(SpecialKeys button)
        {
            return GLWrapper.getKeyDown((int)button);
        }
        /// <summary>
        /// Returns true if the key was lifted during the current frame
        /// <param name="button">keycode to check if lifted</param>>
        /// </summary>
        public bool GetSpecialKeyUp(SpecialKeys button)
        {
            return GLWrapper.getKeyUp((int)button);
        }

        /// <summary>
        /// Returns true if the mouse button is currently held down
        /// <param name="mouseBTN">0 for left click, 1 for right click</param>
        /// </summary>
        public bool GetMouse(int mouseBTN)
        {
            if(mouseBTN != 0 && mouseBTN != 1)
                throw new ArgumentException("mouse button must be 1 or 0");
            return GLWrapper.getMouse(mouseBTN);
        }
        /// <summary>
        /// Returns true if the mouse button was was pressed down during the current frame
        /// <param name="mouseBTN">0 for left click, 1 for right click</param>
        /// </summary>
        public bool GetMouseDown(int mouseBTN)
        {
            if (mouseBTN != 0 && mouseBTN != 1)
                throw new ArgumentException("mouse button must be 1 or 0");
            return GLWrapper.getMouseDown(mouseBTN);
        }
        /// <summary>
        /// Returns true if the mouse button was was lifted up during the current frame
        /// <param name="mouseBTN">0 for left click, 1 for right click</param>
        /// </summary>
        public bool GetMouseUp(int mouseBTN)
        {
            if (mouseBTN != 0 && mouseBTN != 1)
                throw new ArgumentException("mouse button must be 1 or 0");
            return GLWrapper.getMouseUp(mouseBTN);
        }

        /// <summary>
        /// Returns the last numeric key press
        /// </summary>
        public int LastNumberKey { get => lastNumber; }

        private int lastNumber = 0;

        #endregion get key functions

        #region GDIDrawer mouse functions
        /// <summary>
        /// Returns true if there was a left click since the last time the function was called
        /// <param name="Position">the position of the mouse when it was last clicked</param>>
        /// </summary>
        public bool GetLastMouseLeftClick(out vec2 Postion)
        {
            Postion = LastLeftClick;
            if (leftClickToggle)
            {
                leftClickToggle = false;
                return true;
            }
            return false;
        }
        private vec2 iLastLeft = vec2.Zero;
        public vec2 LastLeftClick { get { return iLastLeft; } }
        private bool leftClickToggle = true;

        /// <summary>
        /// Returns true if there was a right click since the last time the function was called
        /// <param name="Position">the position of the mouse when it was last clicked</param>>
        /// </summary>
        public bool GetLastMouseRightClick(out vec2 Postion)
        {
            Postion = LastRightClick;
            if (rightClickToggle)
            {
                rightClickToggle = false;
                return true;
            }
            return false;
        }
        private vec2 iLastRight = vec2.Zero;
        public vec2 LastRightClick { get { return iLastLeft; } }
        private bool rightClickToggle = true;

        /// <summary>
        /// Returns true if there was a left click since the last time the function was called
        /// <param name="Position">the Scaled position of the mouse when it was last clicked</param>>
        /// </summary>
        public bool GetLastMouseLeftClickScaled(out vec2 Postion)
        {
            Postion = LastLeftClick / Scale;
            if (leftClickToggle)
            {
                leftClickToggle = false;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Returns true if there was a right click since the last time the function was called
        /// <param name="Position">the Scaled position of the mouse when it was last clicked</param>>
        /// </summary>
        public bool GetLastMouseRightClickScaled(out vec2 Postion)
        {
            Postion = LastRightClick / Scale;
            if (rightClickToggle)
            {
                rightClickToggle = false;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns true if the mouse has moved (within the canvas) since the last time the function was called
        /// <param name="Position">the current position of the mouse</param>>
        /// </summary>
        public bool GetLastMousePosition(out vec2 Postion)
        {
            Postion = MousePosition;
            if (mouseMovedToggle)
            {
                mouseMovedToggle = false;
                return true;
            }
            return false;
        }
        private bool mouseMovedToggle = false;

        /// <summary>
        /// Returns true if the mouse has moved (within the canvas) since the last time the function was called
        /// <param name="Position">the current, Scaled, position of the mouse</param>>
        /// </summary>
        public bool GetLastMousePositionScaled(out vec2 Postion)
        {
            Postion = MousePosition / Scale;
            if (mouseMovedToggle)
            {
                mouseMovedToggle = false;
                return true;
            }
            return false;
        }
        #endregion GDIDrawer mouse functions

        //LIST: http://www.glfw.org/docs/latest/group__keys.html
       

        //I'm not proud of this, but it only really exsists prove GLDrawer contains every input function found in GDIDrawer 
        Keys IntToKeys(int key)
        {
            Keys code = Keys.KeyCode;
            try {
                //might get lucky here if the key was a letter
                return (Keys)kc.ConvertFromString((char.ToUpper((char)key)).ToString());
            }

            //many of the keys supported in GLFW are NOT support in windows forms keys, so I've included what I could
            //LIST: http://www.glfw.org/docs/latest/group__keys.html
            catch
            {
                switch (key)
                {
                    case (32):
                        code = Keys.Space;
                        break;
                    case (39):
                        code = Keys.OemQuotes; //triggered by GLFW apostrophe
                        break;
                    case (44):
                        code = Keys.Oemcomma;
                        break;
                    case (45):
                        code = Keys.OemMinus;
                        break;
                    case (46):
                        code = Keys.OemPeriod;
                        break;
                    case (47):
                        code = Keys.Divide;
                        break;
                    case (59):
                        code = Keys.OemSemicolon;
                        break;
                    case (91):
                        code = Keys.OemOpenBrackets; // [
                        break;
                    case (92):
                        code = Keys.OemBackslash;
                        break;
                    case (93):
                        code = Keys.OemCloseBrackets; // ]
                        break;
                    case (256):
                        code = Keys.Escape;
                        break;
                    case (257):
                        code = Keys.Enter;
                        break;
                    case (258):
                        code = Keys.Tab;
                        break;
                    case (259):
                        code = Keys.Back; //backspace
                        break;
                    case (260):
                        code = Keys.Insert;
                        break;
                    case (261):
                        code = Keys.Delete;
                        break;
                    case (262):
                        code = Keys.Right;
                        break;
                    case (263):
                        code = Keys.Left;
                        break;
                    case (264):
                        code = Keys.Down;
                        break;
                    case (265):
                        code = Keys.Up;
                        break;
                    case (266):
                        code = Keys.PageUp;
                        break;
                    case (267):
                        code = Keys.PageDown;
                        break;
                    case (268):
                        code = Keys.Home;
                        break;
                    case (269):
                        code = Keys.End;
                        break;
                    case (280):
                        code = Keys.CapsLock;
                        break;
                    case (281):
                        code = Keys.Scroll; //scroll lock
                        break;
                    case (282):
                        code = Keys.NumLock;
                        break;
                    case (283):
                        code = Keys.PrintScreen;
                        break;
                    case (284):
                        code = Keys.Pause;
                        break;
                    case (290):
                        code = Keys.F1;
                        break;
                    case (291):
                        code = Keys.F2;
                        break;
                    case (292):
                        code = Keys.F3;
                        break;
                    case (293):
                        code = Keys.F4;
                        break;
                    case (294):
                        code = Keys.F5;
                        break;
                    case (295):
                        code = Keys.F6;
                        break;
                    case (296):
                        code = Keys.F7;
                        break;
                    case (297):
                        code = Keys.F8;
                        break;
                    case (298):
                        code = Keys.F9;
                        break;
                    case (299):
                        code = Keys.F10;
                        break;
                    case (300):
                        code = Keys.F11;
                        break;
                    case (301):
                        code = Keys.F12;
                        break;
                    case (302):
                        code = Keys.F13;
                        break;
                    case (303):
                        code = Keys.F14;
                        break;
                    case (304):
                        code = Keys.F15;
                        break;
                    case (305):
                        code = Keys.F16;
                        break;
                    case (306):
                        code = Keys.F17;
                        break;
                    case (307):
                        code = Keys.F18;
                        break;
                    case (308):
                        code = Keys.F19;
                        break;
                    case (309):
                        code = Keys.F20;
                        break;
                    case (310):
                        code = Keys.F21;
                        break;
                    case (311):
                        code = Keys.F22;
                        break;
                    case (312):
                        code = Keys.F23;
                        break;
                    case (313):
                        code = Keys.F24;
                        break;
                    case (320):
                        code = Keys.NumPad0;
                        break;
                    case (321):
                        code = Keys.NumPad1;
                        break;
                    case (322):
                        code = Keys.NumPad2;
                        break;
                    case (323):
                        code = Keys.NumPad3;
                        break;
                    case (324):
                        code = Keys.NumPad4;
                        break;
                    case (325):
                        code = Keys.NumPad5;
                        break;
                    case (326):
                        code = Keys.NumPad6;
                        break;
                    case (327):
                        code = Keys.NumPad7;
                        break;
                    case (328):
                        code = Keys.NumPad8;
                        break;
                    case (329):
                        code = Keys.NumPad9;
                        break;
                    case (330):
                        code = Keys.Decimal; //numpad decimal
                        break;
                    case (331):
                        code = Keys.Divide; //numpad divide
                        break;
                    case (332):
                        code = Keys.Multiply; //numpad multiply
                        break;
                    case (333):
                        code = Keys.Subtract; //numpad subtract
                        break;
                    case (334):
                        code = Keys.Add; //numpad add
                        break;
                    case (335):
                        code = Keys.Enter; //numpad enter
                        break;
                    case (340):
                        code = Keys.LShiftKey;
                        break;
                    case (341):
                        code = Keys.LControlKey;
                        break;
                    case (342):
                        code = Keys.Alt;
                        break;
                    case (344):
                        code = Keys.RShiftKey;
                        break;
                    case (345):
                        code = Keys.RControlKey;
                        break;
                    case (346):
                        code = Keys.Alt; //windows forms keys does not suport right and left alt, so they map to the same key
                        break;
                    case (348):
                        code = Keys.Menu;
                        break;
                    default:
                        //throw new ArgumentException("Key not supported by Windows.Forms.Keys. Try using GetKey functions");
                        break;
                }
            }
            return code;
        }
    }
    public enum SpecialKeys
    {
        UNKNOWN = -1,
        SPACE = 32,
        APOSTROPHE = 39,
        COMMA = 44,
        MINUS = 45,
        PERIOD = 46,
        FORWARDSLASH = 47,
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,
        SEMICOLON = 59,
        EQUALS = 61,
        LEFTBRACKET = 91,
        BACKSLASH = 92,
        RIGHTBRACKED = 93,
        GRAVEACCENT = 96,
        ESCAPE = 256,
        ENTER = 257,
        TAB = 258,
        BACKSPACE = 259,
        INSERT = 260,
        DELETE = 261,
        RIGHT = 262,
        LEFT = 263,
        DOWN = 264,
        UP = 265,
        PAGEUP = 266,
        PAGEDOWN = 267,
        HOME = 268,
        END = 269,
        CAPSLOCK = 280,
        SCROLLLOCK = 281,
        NUMLOCK = 282,
        PRINTSCREEN = 283,
        PAUSE = 84,
        F1 = 290,
        F2 = 291,
        F3 = 292,
        F4 = 293,
        F5 = 294,
        F6 = 295,
        F7 = 296,
        F8 = 297,
        F9 = 298,
        F10 = 299,
        F11 = 300,
        F12 = 301,
        F13 = 302,
        F14 = 303,
        F15 = 304,
        F16 = 305,
        F17 = 306,
        F18 = 307,
        F19 = 308,
        F20 = 309,
        F21 = 310,
        F22 = 311,
        F23 = 312,
        F24 = 313,
        F25 = 314,
        NP0 = 320,
        NP1 = 321,
        NP2 = 322,
        NP3 = 323,
        NP4 = 324,
        NP5 = 325,
        NP6 = 326,
        NP7 = 327,
        NP8 = 328,
        NP9 = 329,
        NPDECIMAL = 330,
        NPDIVIDE = 331,
        NPMULTIPLY = 332,
        NPSUBTRACT = 333,
        NPADD = 334,
        KPENTER = 335,
        KPEQUAL = 336,
        LEFTSHIFT = 340,
        LEFTCONTROL = 341,
        LEFTALT = 342,
        LEFTSUPER = 343,
        RIGHTSHIFT = 344,
        RIGHTCONTROL = 345,
        RIGHTALT = 346,
        RIGHTSUPER = 347,
        MENUE = 348
    };
}
