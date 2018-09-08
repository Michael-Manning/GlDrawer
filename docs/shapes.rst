Shapes
=========

In these docs, a shape is used to describe anything that can be drawn on a canvas. 
In GLDrawer, a Shape is an object inheritance tree which has a similar definition.
While objects and inheritance are out of the scope of this guide, it will be described here briefly
as having general understanding will make your life *much* easier.

**Drawing Shapes without a reference**

When adding a rectangle to a canvas, you could do so like this:

.. code-block:: C#

    //adds a rectangle at position 100,100 with a width and height of 200
    canvas.AddCenteredRectangle(100, 100, 200, 200, Color.White);

If you decided that after waiting for an input, you would now like that rectangle to move 100 pixels to right, 
you are forced to do so like this:

.. code-block:: C#

    canvas.AddCenteredRectangle(100, 100, 200, 200, Color.White);
    Console.ReadKey();
    canvas.Clear();
    canvas.AddCenteredRectangle(200, 100, 200, 200, Color.White);

What the above code is doing, is creating a rectangle, then after an input, removing that rectangle and creating
a new one 100 pixels further to the right. This works, but in GLDrawer, there are better ways to accomplish this.

**Drawing Shapes with an object reference**

A GLDrawer canvas itself is actually an object. This is because we can create an instance of a canvas on on line of code,
and then do something else with it on a different one. In the exact same way, we can create an instance of a Rectangle,
then add it to a canvas later. Adding a rectangle to a canvas while keeping a reference can be done in two ways.
The following examples do the same thing as the previous example, but with references.

.. code-block:: C#

    Rectangle shrecktangle = new Rectangle(new vec2(100,100), new vec2(200,200), Color.White);
    canvas.Add(shrecktangle);
    Console.ReadKey();
    shrecktangle.Position = new vec2(200, 100);

As you can see above, we create a Rectangle *object*, then add it to the canvas on the next line of code.
Unlike before, we actually get to keep a *reference* of that rectangle called shrecktangle. If we would like to
move the shrecktangle, we don't have to recreate it; we only need to change its *Position* property. 
This can also be done with the following shortcut:

.. code-block:: C#

    Rectangle shrecktangle = canvas.AddCenteredRectangle(100, 100, 200, 200, Color.White);
    Console.ReadKey();
    shrecktangle.Position += new vec2(100, 0);

The GLDrawer *AddCenteredRectangle* function not only adds a rectangle to a canvas, it actually returns a reference!
With this function, the Rectangle reference is always created behind the scenes, and you can choose wether or not to  keep it.
Another "shortcut" is instead of creating a new vec2 for position, we can alternatively increment it by a vec2 with an x value.

**How can there be Rectangles AND Shapes? (advanced)**

If the idea of creating a Rectangle you can use makes sense to you, it likely stands to reason you could also create 
an Ellipse, Line, Polygon .ect the same way. Seemingly unneeded, in addition to these object types, there is also a *Shape* type.

Your understanding of this isn't required to use GLDrawer, but in short: all Rectangles are Shapes, but not all Shapes are Rectangles.

You cannot create a standalone Shape in the same way as a Rectangle or an Ellipse, but because they are both members of the 
shape *hierarchy*, we are allowed to make some assumptions. We can use the *canvas.Add()* function to add any shape, regardless of what kind.
You can also create a list of shapes eg. List<Shape> shapes, and store both Rectangles *and* Ellipses in it.

Creating a list like this would be considered a more advanced use of the shape hierarchy. That being said, there are benefits thanks to the Shape type.
Regardless of what spesific *type* of shape a Shape actually is, we can assume it will have all the members seen in the top level member list.
This means that even though you cannot *create* a generic Shape without knowing what kind it is, if you already have one, 
like in a list, you can still change it. If you have an generic Shape, you can't change its filepath, but you can still change it's position.
This is because not all shapes, have filepaths, but all shapes *do* have a location.


.. toctree::
   :maxdepth: 1
   
   shapetypes-shapes.rst
   fields-shapes.rst