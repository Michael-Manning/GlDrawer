Members
=========

**Fields**

==================  =======  ===================
Name                Type      Description    
==================  =======  ===================
Time                long     Time in seconds since canvas creation
DeltaTime           long     Time in seconds since previous frame
Width               int      Width of the canvas in pixels
Height              int      Height of the canvas in pixels
WindowTitle         string   Text displayed on the top window bar
InvertedYAxis       bool     Wether screen coordinates zero point starts on the top or bottom of the canvas 
Scale               float    Multiplier for all coordinates such as shape positions and scale
BackColor           Color    The color of the canvas background or back buffer
SimpleBackBuffer    bool     Forces the back buffer to be a solid color
ExtraInfo           bool     Adds information such as FPS, render time, and shape count to the window title
VSync               bool     Limits the framerate to 60fps and waits for vertical screen synchronization (read only)
DebugMode           bool     Overlays text displaying render times and shape count of the canvas
Borderless          bool     wether the canvas window has Borders
AutoRender          bool     wether the canvas will render the adde shapes automatically
Center              vec2     Property that returns the center point of the canvas
ShapeCount          int      Property that returns the number of shapes on the canvas at any given time
Update              event    Event Action which is invoked once per frame
==================  =======  ===================

|

**Functions**

====================  ===========  ===================
Name                  Type         Description    
====================  ===========  ===================
Add                   Shape        Adds any shape to the canvas and returns a copy of the reference
RemoveShape           void         Removes a shape from the canvas given a reference
Refresh               void         Removes any lingering shapes from the canvas which have no references
SendBack              void         Sets a shape one index earlier in the drawing order
Sendforward           void         Sets a shape one index later in the drawing order
SendToBack            void         sets a shape to the first item in the drawing order
SentToFront           void         sets a shape to the last item in the drawing order
SwapDrawOrder         void         Switches the drawing order placement of two shapes on a canvas
setBBPixel            void         Sets the color of a single pixel in the background
setBBShape            void         Draws a shape to the background with no reoccurring performance cost and returns a reference
getPixel              Color        gets the color of a single pixel on the canvas
ClearBackBuffer       void         Redraws the background with the background color
Clear                 void         Removes all shapes from the canvas
Close                 void         Closes the canvas window or removes the canvas from a forms panel
SetWindowPostion      void         Moves the canvas window to a new location on the screen
AddRectangle          Rectangle    Adds a Rectangle to the canvas and returns a reference
AddCenteredectangle   Rectangle    Adds a centered Rectangle to the canvas and returns a reference
AddEllipse            Ellipse      Adds an Ellipse to the canvas and returns a reference
AddCenteredEllipse    Ellipse      Adds a centered Ellipse to the canvas and returns a reference
AddLine               Line         Adds a Line to the canvas where start and end points are defined and returns a reference
AddLine               Line         Adds a line segment to the canvas at a known start point using a length and rotation angle
AddSprite             Sprite       Adds an image to the canvas given a filepath and returns a reference
AddCenteredSprite     Sprite       Adds a centered image to the canvas given a filepath and returns a reference
AddText               Text         Adds text to the canvas given a bounding box and returns a reference
AddCenteredText       Text         Adds centered text to the canvas and returns a reference
AddCenteredPolygon    Polygon      Adds a centered polygon to the canvas and returns a reference
====================  ===========  ===================