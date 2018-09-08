Using vec2
===========

A vec2 is a simple struct containing nothing but an X and Y float. It is similar to a Pointf in that regard, but is packed 
with mathematical features and shortcuts.

You can create a vec2 with its standard constructor:

.. code-block:: C#
   
   //creates a vec2 with an X value of 100.0, and a Y value of 200.0
   vec2 location = new vec2(100f, 200f);

Giving a vec2 one parameter will assign the value to both X and Y.


.. code-block:: C#
   
   //creates a vec2 with an X and Y value of 100.0
   vec2 location = new vec2(100f);

You can add, subtract, multiply, and divide vec2s together. The following two code blocks perform the same operations.

.. code-block:: C#
   
   vec2 A = new vec2(10f, 20f);
   vec2 B = new vec2(5f, 2f);

   //creates a vec2 with an X value of 15.0, and a Y of 22.0
   vec2 C = new vec2(A.x + B.x, A.y + B.y);

Is the same as..

.. code-block:: C#
   
   vec2 A = new vec2(10f, 20f);
   vec2 B = new vec2(5f, 2f);

   vec2 C = A + B

vec2 math operators can also be performed with floats to affect both X and Y

.. code-block:: C#
   
   vec2 A = new vec2(5f, 10f);

   //A now has an X value of 30.0, and a Y of 60.0
   vec2 A *= 6f;

Calling *vec2.Length* will return the distance between vec2s in pixels

.. code-block:: C#
   
   vec2 A = new vec2(); //same as vec2.Zero
   vec2 B = new vec2(5f, 10f);
   
   //equivalent to 11.18 or Math.sqrt((A.x-B.x)*(A.x-B.x)+(A.y-B.y)*(A.y-B.y))
   float distance = A.Length(B);

Calling *vec2.Normalize* will return a direction vector from (0,0) with a radius of 1.0

.. code-block:: C#
   
   vec2 position = new vec2(100f, 150f);
   vec2 destination = new vec2(50f, 30f);
   float moveDistance = 3f;

   vec2 moveDirection = (position - destination).Normalize();

   //position will be exactly 3 pixels closer to destination
   position += moveDirection * moveDistance;

vec2 can be implicitly converted between Pointf as well as rounded down to a standard Point.

.. code-block:: C#
   
   Pointf A = new Pointf(100f, 200f);
   vec2 B = A; //X and Y from A get assigned to B

   //If you need to use Pointf and are feeling lazy, casting as a vec2 allows use of all math operators
   Pointf C = (vec2)A + B;

.. toctree::
   :maxdepth: 1
   
   fields-vec2.rst