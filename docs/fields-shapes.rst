Members
============

|

**Top level properties**

==================  ==========  ============
Name                Type        Description
==================  ==========  ============ 
Position            vec2        Location of the Shape on the canvas in pixels
Scale               vec2        Width and Height of the Shape in pixels
Angle               float       The angle of the Shape in radians
FillColor           Color       Color of the Shape
BorderColor         Color       Color of the Shapes' outline
BorderWidth         float       Width of the Shapes' outline in pixels
RotationSpeed       float       Animates the rotation of the Shape
Hidden              bool        If true, the shape will not be drawn
==================  ==========  ============

|

**Bottom level properties**

==================  ==================  ============  ============ 
Name                Type                ShapeType     Description
==================  ==================  ============  ============ 
Thickness           float               Line          Width of the Line
Length              float               Line          Length of the Line
Start               vec2                Line          Location of one of the Line's ends
End                 vec2                Line          Location of one of the Line's ends
SideCount           int                 Polygon       Number of sides to draw the Polygon Width
FilePath            string              Sprite        Path of the image file to be drawn
Justification       JustificationType   Text          Justification of the paragraph
Bound               Rectangle           Text          An optional boundry box to format a paragraph within
Body                string              Text          The string of text to be drawn
Font                string              Text          The filepath to the ttf file to use. (Default Times new Roman)
==================  ==================  ============  ============ 