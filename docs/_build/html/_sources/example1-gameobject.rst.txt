1-Circle on mouse
====================

**Getting a circle on the screen**

First things first, we need to create a class for our GameObject. If you're unfamiliar
with inheritance, this will look new to you. We can't just create a GameObject like a 
variable, we have to create our own class and say it's a GameObject in the definition
using " : GameObject" near the name.


.. code-block:: C#

    class Ball : GameObject
    {
        public override void Start()
        {
            //Gets run once when a ball is added to the canvas
        }
        public override void Update()
        {
            //Gets called once per frame
        }
    }

The above class is a bare bones GameObject and already there are some new things here.
By setting the ball's base class as a GameObject, it gets all the features of the GameObject
class. The virtual function you will likely use the most are Start and Update. These
functions will be called by the canvas and most of your game's code will be inside of 
update functions.

**Important Note**

Avoid class constructors as much as possible. Any GameObject code not invoked by the canvas
can be dangerous as it is possible to use canvas functions before it has been initialized.
If you need to add GameObjects at different position, use Canvas.Instatiate which is designed
for that and will be covered later. 

.. code-block:: C#

    class Ball : GameObject
    {
        public override void Start()
        {
            AddChildShape(new Polygon(vec2.Zero, new vec2(100), 0, 1, Color.White));
        }
        public override void Update()
        {
            transform.Position = Canvas.MousePositionScreenSpace;
        }
    }

Now in your main program

.. code-block:: C#

    can.Add(new Ball());

This will create a white Ellipse that will follow your mouse. Note the use of "transform" and "Canvas".
Every GameObject has a Transform type called transform and a reference to the canvas called Canvas build in. 