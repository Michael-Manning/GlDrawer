3-Invoke Repeating and Timers
============================================

When programming a game, you will regularly find yourself
needing time dependant functionality. Games need many cycle based
and delayed functions, but you can't just sleep the thread without 
stopping the entire programming. We will cover some timing techniques
as well as built in tools in GLDrawer.

**Delta Time**

At any time, you can read from DeltaTime which is every GameObject as well as the canvas they are on.
DeltaTime is the amount of time in seconds since the last frame. This is useful in 
variety of situations. One being movement due to the fact that the framerate of your game changes.
If you add one to the x coordinate of an object every frame, it might look smooth at 60 frames per second,
but when it dips below that, the object will actually move slower with the framerate. If you multiply that 
movement by DeltaTime, it will compensate for dropped frames and end up in the same place regardless of FPS.

DeltaTime can also be used to create a simple timer by incrementing a variable:

.. code-block:: C#

    class ThingWithTimer : GameObject
    {
        float timer = 0;
        public override void Update()
        {
            timer += DeltaTime;
            if (timer > 2f)
            {
                //this gets run once every two seconds

                timer = 0;
            }
        }
    }

By accumulating DeltaTime, we are effectively creating keeping track of time locally.
Unlike GLCanvas.Time which never stops increasing, our local timer gets whenever it 
accumulates enough time.

**Canvas Invoke**

An easy way to create a delayed call is to use the Invoke function of the canvas. It takes
an Action which means you can pass in a method or an anonymous function.

.. code-block:: C#

    class ThingWithTimer : GameObject
    {
        public override void Start()
        {
            Canvas.Invoke(delayed, 2f);
            Canvas.Invoke(delegate { Console.WriteLine("It's been 4 seconds"); }, 4f);
        }

        public void delayed()
        {
            //this gets called once after two seconds
        }
    }

Similar to GLCanvas.Invoke is InvokeRepeating. This function is the same as invoke, but will call 
your function over and over until it's destroyed or the program ends. The following code will 
accomplish the same result as the first timer example using DeltaTime.



    class ThingWithTimer : GameObject
    {
        public override void Start()
        {
            Canvas.InvokeRepeating(RepeatedFunction, 2f);
        }

        public void RepeatedFunction()
        {
            //this gets called every 2 seconds
        }
    }

This approach has it's downsides if you need something more complex. The timing of InvokeRepeating
is fixed. If you want a timer that can be easily turned on and off or have delays that can change
on the fly, you should stick to the DeltaTime Method.

**GameObject.Destroy**

Now to leave you with an example. Destroy will destroy the GameObject and clean up it's memory.
Sticking to the timing theme, it also accepts a delay parameter for how long to wait before destruction.
This is useful for temporary objects such as an explosion effect since you can Instantiate a GameObject 
with a ParticleSystem child and immediately call destroy with a delay. This Lets you do what ever you want
with whatever called Instantiate and ensures the explosion animation can carry out before Destroying itself. 

.. code-block:: C#

    class Projectile : GameObject
    {
        public override void Start()
        {
            //set drawing element and destroy after 3.5 seconds
            AddChildShape(new Polygon(vec2.Zero, new vec2(20), 0, 1, Color.Red));
            Destroy(3.5f);
        }
        public override void Update()
        {
            //move to the right of the screen
            transform.Position += new vec2(3, 0);
        }
    }
    class Spawner : GameObject
    {
        Projectile bullet = new Projectile();
        public override void Start()
        {
            //clone the bullet every 0.15 seconds at the current position
            Canvas.InvokeRepeating(delegate { Instantiate(bullet, transform.Position);}, 0.15f);
        }
        public override void Update()
        {
            //change spawn location over time
            transform.Position = new vec2(-350, 200 * (float)Math.Sin(Time));
        }
    }


.. image:: images/timerdemo.gif
   :width: 400px
   :height: 300px
   :scale: 100 %
   :alt: parent transform demo
   :align: left

