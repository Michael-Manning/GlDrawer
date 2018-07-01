# GLDrawer
Simple .net drawing and game creating interface. 

Inspired by the GDIDrawer by Simon Walker which is a .Net wrapper for GDI to make programming assignments more interesting for NAIT students. 
GLDrawer is a from-scratch creation using Gl3w, GLFW, STB, and Box2DNet. It is being made to contain all the features found in GDIDrawer, but with games in mind.

Differences from GDIDrawer include:
- Higher performance
- Ability to use shapes as objects, even while on a canvas 
- Addition of sprites for easy use of image files as well as extra shape types
- anti aliasing, mag mapping and min mapping
- scaling of shapes after their creation
- rotation of shapes
- The addition of Gameobjects which allow for the use of world coordinates in the form of transforms
- Timing utilities such as delta times and on frame events
- Math utilities such a linearly interpolated motion
- Colliders with collision/point intersection detection (in progress
- particle engine with physics (in progress)
- Implementation and wrapper of the Box2D physics engine (in progress)
- velocity and motion tools

This Repo was originally created in october of 2017, but has been recently restarted from the ground up with native C++. 
I learned so much in the first attempt that it made more sense to remake the backend in true OpenGL instead of the wrapper I was using.




