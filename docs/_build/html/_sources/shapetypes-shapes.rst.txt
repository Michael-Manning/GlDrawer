Shape Types
============

There are six different types of shapes that can be created with GLDrawer.

**Rectangle**

.. image:: images/rectangleExample.png
   :width: 220px
   :height: 220px
   :scale: 100 %
   :alt: rectangle image
   :align: left

A Rectangle has no special shape properties, but you can never go wrong with the fundamentals.

|
|
|
|
|
|
|

**Ellipse**

.. image:: images/ellipseExample.png
   :width: 220px
   :height: 220px
   :scale: 100 %
   :alt: ellipse image
   :align: left

Fun fact: in the back end, an Ellipse is the exact same datatype as a Rectangle, but the sidecount is set to one instead of four.

The only reason you see a circle instead of a square is because the shader running on your graphics processor knows when to draw
round corners.
 
|
|
|

**Line**

.. image:: images/lineExample.png
   :width: 220px
   :height: 220px
   :scale: 100 %
   :alt: line image
   :align: left

A Line actually just creates another Rectangle in the backend. The reason lines are usefull is because you can modify 
the start and end locations individually and the resulting position, scale, and angle of the resulting Rectangle
are automatically calculated. 

|
|
|
|
|
|

**Polygon**

.. image:: images/polygonExample.png
   :width: 220px
   :height: 220px
   :scale: 100 %
   :alt: polygon image
   :align: left

The three shape types above are also polygons, but the *Polygon* is a polygon that can be drawn with any number of sides.
It is far less precise than the other shapes because the width and height of the polygon is dependant on the number of sides
it has as well as its angle.

A Polygons will almost always be drawn slightly smaller than its width and height values.

|
|
|

**Sprite**

.. image:: images/spriteExample.png
   :width: 220px
   :height: 220px
   :scale: 100 %
   :alt: sprite image
   :align: left

Unlike in GDIDrawer which requires you to set the back buffer pixel by pixel to create an image, a GlDrawer Sprite
acts like a Rectangle you can show an image with.

Sprites render just as quickly as the other shape types, and you can use them to make tiled textures in small games.

Sprites work with most image file types and transparency *is* supported.

|

**Text**

.. image:: images/textExample.png
   :width: 220px
   :height: 220px
   :scale: 100 %
   :alt: text image
   :align: left

GLDrawer has the ability rasterize TrueTypeFont files. This means that with nothing but the font filepath, and a string of text,
you can generate an image for every letter of the alphabet and have the letters appear in the same order as your string.


