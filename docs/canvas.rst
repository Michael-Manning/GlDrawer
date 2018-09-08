.. _canvas-label:

The Canvas
=============

A canvas is a virtual surface on which shapes can be drawn. 
This section describes How the canvas system works and the two ways in which you can create one.

**Standalone window vs embedded**

Just like in GDIDrawer, a canvas window is an actual, seperate window that contains nothing but a canvas.
To get this window to appear, it's common to start with console application and create it from there.

An *embedded* window is exclusively for windows forms applications. It lets you create a canvas 
within a form instead of creating two separate windows.

**A note about buffers**

The canvas system in GLDrawer uses two different buffers. 
A buffer is a dynamic block of memory which is used to store the pixel values to display on screen.
When you add normal shapes the the canvas, they are drawn to the *front buffer*. The front buffer is cleared every frame 
and all the shapes you've added to the screen are redrawn in case your changed something about them.

The *Back Buffer* is shown behind the front buffer and is never redrawn unless done manually or if the window is reSized.
The backBuffer is like Microsoft paint. Any pixels you change will stay that way, but if you change the same pixel twice, you lose the first one.

You can also draw standard shapes to the back buffer. Adding shapes to the front buffer slows down your program the more you add because
they all get redrawn every frame.
shapes pasted to the back buffer are only ever drawn once, and will never slow down your program, no matter how many you add.
The drawback is that you cannot move or change any shape once it's been drawn to the backBuffer.

.. image:: images/BBdemo.gif
   :width: 1300px
   :height: 900px
   :scale: 50 %
   :alt: backBuffer demo
   :align: left
   
Adding thousands of shapes per second to the back buffer with no performance hit

.. toctree::
   :maxdepth: 1
   
   basic-canvas.rst
   forms-canvas.rst
   fields-canvas.rst
   
   

   
   