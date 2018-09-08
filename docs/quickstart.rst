Quick Start
============

To install GLDrawer in your project follow these steps.

| **Step 1**
| Download The .DLL from the Github repository `here.`_

.. _here.: https://github.com/Michael-Manning/GLDrawer

| **Step 2**
| Extract the files, locate the latest release of GLDrawer, then copy GLDrawer.DLL file.
| Place the .DLL file somewhere easy to find or in your project directory.

| **Step 3**
| Open your project. In the solution explorer, right click references, then click add reference.
| Click Browse, navigate to GLDrawer.DLL and hit okay.

| **Step 4**
| Include the GLDrawer namespece in your file with the "using" keyword and test the following program.

.. code-block:: C#

    using GLDrawer;
 
    namespace myProject
    { 
        public static class Program
        {
            static void Main(string[] args)
            {
                GLCanvas can = new GLCanvas();
                can.AddText("It Works :)");
                Console.ReadKey(); 
            }
        }
    }

A window should will pop up and you should see the "it works" text. 
If you did, GLDrawer has been installed successfully!