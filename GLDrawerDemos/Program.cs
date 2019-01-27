using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawer;
using System.Threading;

namespace GLDrawerDemos
{
    public static class Program
    {
        static void Main(string[] args)
        {
            //platformGame.SimplePlatformer.run();
            //platformGame.AdvancedPlatformer.run();
            // GLDrawerDemos.levelEditorProgram.run();
            //spaceGame.spaceGame.run();
            //TextEditor.run();

            //demos.FormTest();
            //demos.FastRemoval();
            //demos.BackBufferTest();
            //demos.IntersectTest();
            //demos.PhysicsTest();       
            //demos.BackBufferMultithread();

            // tempim();


            GLCanvas can = new GLCanvas(LegacyCoordinates: false);

            var e = can.AddCenteredEllipse(0, 0, 20, 20, Color.White);
            var text = can.AddCenteredText("test", 40, Color.White);
            bool inverted = false;

            can.Update += delegate {
                e.Position = can.MousePositionScaled;
                if(can.GetKeyDown('c'))
                {
                    inverted = !inverted;
                    if (inverted)
                    {
                        can.SetInvertedCoordinates();
                        text.Position = can.Center;
                    }

                    else
                    {
                        can.SetCartesianCoordinates();
                        text.Position = vec2.Zero;
                    }

                }
                text.Body = string.Format(
                    "Press C to toggle Coordinates: {0}\n X: {1} Y: {2}",
                     inverted ? "Legacy" : "Cartesian", can.MousePosition.x, can.MousePosition.y
                    );
            };
            
            // GLCanvas can = new GLCanvas();

            //can.AddLine(100, 0, -100, 0, 5, Color.White);
            //can.AddLine(0, -100, 0, 5, 5, Color.Red);
            //can.AddLine(vec2.Zero, 100, 2, 5, Color.Blue);
            //Polygon p = new Polygon(vec2.Zero, new vec2(100), 2, 4, Color.Green);
            //p.RotationSpeed = 1.0f;
            //can.Add(p);



            // can.SetInvertedCoordinates();
            // can.AddCenteredText("test", 50, 100, 20, Color.White);
            //  Polygon p = can.AddCenteredEllipse(0, 30, 100, 100, Color.White) as Polygon;

            // can.Update += () => can.CameraScale = new vec2(can.CameraScale.x, (float)Math.Sin(can.Time) * 2);



            //  can.Close();
            ////  Console.ReadKey();
            //  // Polygon p = new Polygon(vec2.Zero, vec2.Zero);
            //  Thread.Sleep(1000);
            Console.ReadKey();
        }

        static void tempim()
        {
            int dims = 1000;
            GLCanvas can = new GLCanvas(dims, dims, BackColor: Color.White, VSync: false);
            can.CameraPosition = can.Center;
            int padding = 100;
            int lines = 10;

            int div = (dims - padding) / lines;
            for (int i = 0; i < lines; i++)
            {
                float dist = padding + div * i;
                can.AddLine(padding, dist, dims - padding, dist, 4, Color.Black);
                can.AddLine(dist, padding, dist, dims - padding, 4, Color.Black);
            }

            Console.ReadKey();
                 can.WriteToBMP("../coolfile");
          //  can.GetPixel(100, 100);
            //can.tempF();
            Console.ReadKey();
        }
    }
}
