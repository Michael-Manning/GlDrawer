Members
============

|

**Delegates**

==================  ============
Name                Parameters
==================  ============  
GLMouseEvent        (vec2 Position, GLCanvas Canvas)     
GLKeyEvent          (Keys Code, GLCanvas Canvas)  
==================  ============ 

|

**Events**

=======================  =============  ===================
Name                     Type             Description    
=======================  =============  ===================
MouseLeftClick           GLMouseEvent   Left click somewhere on the Canvas
MouseRightClick          GLMouseEvent   Right click somewhere on the Canvas
MouseLeftClickScaled     GLMouseEvent   Left click somewhere on the Canvas multiplied by the canvas scale factor
MouseRightClickScaled    GLMouseEvent   Right click somewhere on the Canvas multiplied by the canvas scale factor
MouseLeftRelease         GLMouseEvent   Left mouse button released somewhere on the canvas
MouseRightRelease        GLMouseEvent   Right mouse button released somewhere on the canvas
MouseLeftReleaseScaled   GLMouseEvent   Left mouse button released somewhere on the canvas multiplied by the canvas scale factor
MouseRightReleaseScaled  GLMouseEvent   Right mouse button released somewhere on the canvas multiplied by the canvas scale factor
MouseMove                GLMouseEvent   The mouse position is changed while over the canvas 
MouseMoveScaled          GLMouseEvent   The mouse position is changed while over the canvas. Delegate vec2 is multiplied by the canvas scale factor
KeyDown                  GLKeyEvent     A key was pressed down with the canvas window in focus
KeyUp                    GLKeyEvent     A key was released with the canvas window in focus
=======================  =============  ===================

|

**Properties**

====================  =====  ===================
Name                  Type   Description    
====================  =====  ===================
MousePosition         vec2   Location of the mouse in pixels relative to the canvas
MousePositionScaled   vec2   Location of the mouse in pixels relative to the canvas, multiplied by the canvas scale factor
LastLeftClick         vec2   The Location of the Last left click in pixels relative to the canvas
LastRightClick        vec2   The Location of the Last Right click in pixels relative to the canvas
LastNumberKey         int    The Last number key pressed with the canvas window in focus
====================  =====  ===================

|

**Functions**

============================  ======  ===========  ================
Name                          Type    Parameters   Description
============================  ======  ===========  ================ 
GetKey                        bool    char         Returns true if a given ASCII key is being held down
GetKeyDown                    bool    char         Returns true if a given ASCII key was pressed on the current frame
GetKeyUp                      bool    char         Returns true if a given ASCII key was released on the current frame
GetSpecialKey                 bool    keycode      Returns true if a given Keycode is being held down
GetSpecialKeyDown             bool    char         Returns true if a given Keycode key was pressed on the current frame
GetSpecialKeyUp               bool    char         Returns true if a given Keycode key was released on the current frame
GetLastMouseLeftClick         bool    out vec2     (Legacy) Returns true if there was a left click since the last time the function was called
GetLastMouseRightClick        bool    out vec2     (Legacy) Returns true if there was a Right click since the last time the function was called
GetLastMouseLeftClickScaled   bool    out vec2     (Legacy) Returns true if there was a left click since the last time the function was called. out vec2 is multiplied by the canvas scale factor
GetLastMouseRightClickScaled  bool    out vec2     (Legacy) Returns true if there was a Right click since the last time the function was called. out vec2 is multiplied by the canvas scale factor
GetLastMousePosition          bool    out vec2     (legacy) Returns true if the mouse has moved (within the canvas) since the last time the function was called (same as MousePosition)
GetLastMousePositionScaled    bool    out vec2     (legacy) Returns true if the mouse has moved (within the canvas) since the last time the function was called. out vec2 is multiplied by scale
============================  ======  ===========  ================

|

**keycode values**

Also found on the GLFW docs: http://www.glfw.org/docs/latest/group__keys.html

====================  ===================  =========
Name                  Enum/ASCII value      Note
====================  ===================  =========
UNKNOWN               -1                    
SPACE                 32                    Spacebar
APOSTROPHE            39                    '
COMMA                 44                    ,
MINUS                 45                    \-
PERIOD                46                    .
FORWARDSLASH          47                    /
D0                    48                    0
D1                    49                    1
D2                    50                    2
D3                    51                    3
D4                    52                    4
D5                    53                    5
D6                    54                    6
D7                    55                    7
D8                    56                    8
D9                    57                    9
SEMICOLON             58                    ;
EQUALS                61                    =
LEFTBRACKET           91                    [
BACKSLASH             92                    \
RIGHTBRACKED          93                    ]
GRAVEACCENT           96                    `
ESCAPE                256
ENTER                 257
TAB                   258
BACKSPACE             259
INSERT                260
DELETE                261
RIGHT                 262
LEFT                  263
DOWN                  264
UP                    265
PAGEUP                266
PAGEDOWN              267
HOME                  268
END                   269
CAPSLOCK              280
SCROLLLOCK            281
NUMLOCK               282
PRINTSCREEN           283
PAUSE                 84
F1                    290
F2                    291
F3                    292
F4                    293
F5                    294
F6                    295
F7                    296
F8                    297
F9                    298
F10                   299
F11                   300
F12                   301
F13                   302
F14                   303
F15                   304
F16                   305
F17                   306
F18                   307
F19                   308
F20                   309
F21                   310
F22                   311
F23                   312
F24                   313
F25                   314
NP0                   320                   Numpad 0
NP1                   321                   Numpad 1
NP2                   322                   Numpad 2 
NP3                   323                   Numpad 3
NP4                   324                   Numpad 4
NP5                   325                   Numpad 5
NP6                   326                   Numpad 6
NP7                   327                   Numpad 7
NP8                   328                   Numpad 8
NP9                   329                   Numpad 9
NPDECIMAL             330                   Numpad .
NPDIVIDE              331                   Numpad /
NPMULTIPLY            332                   Numpad *
NPSUBTRACT            333                   Numpad -
NPADD                 334                   Numpad +
KPENTER               335                   
KPEQUAL               336                   Numpad =
LEFTSHIFT             340
LEFTCONTROL           341
LEFTALT               342
LEFTSUPER             343
RIGHTSHIFT            344
RIGHTCONTROL          345
RIGHTALT              346
RIGHTSUPER            347
MENUE                 348
====================  ===================  =========
