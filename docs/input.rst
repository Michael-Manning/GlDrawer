Input
=======

The input system in GLDrawer lets you find out what the keyboard and mouse are doing 
when using your program. There are a large number of options available for reading input 
information and the functions you choose will depend on the workflow you prefer.

**Event style input**

Because GLDrawer has been modeled after GDIDrawer, all input functions that it has can also be found in GLDrawer.
This style of input detection uses a delegate event subscription system where you can create functions that will
be automatically called by GLDrawer whenever a specific type of input occurs. 

**Game style input**

GLDrawer has been outfitted with many other simple input checks. When using GLDrawer to program a game,
you will likely have script style classes and functions which run ever frame. Being forced to use events to 
find out which keys are being held down is an inconvenience that has to be worked around when structuring code this way.
To make life easier, GLDrawer has a set of functions and properties that will let you find out where the mouse is at any
given time without havening to create new events for every type of input.

.. toctree::
   :maxdepth: 1
   
   events-input.rst
   fields-input.rst