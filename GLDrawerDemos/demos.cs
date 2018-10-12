using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GLDrawer;

namespace GLDrawerDemos
{
    static class demos
    {
        static GLCanvas can;

        /// <summary>
        /// adds 7200 shapes to the screen to test thread safety and performance
        /// </summary>
        public static void groupAdd()
        {
            can = new GLCanvas(1200, 600, TitleDetails: true, VSync: true);
            can.simpleBackBuffer = true;

            for (int i = 0; i < 1200; i++)
                for (int j = 0; j < 6; j++)
                    can.AddCenteredEllipse(i, 50 + j * 100, 100, 100, Color.Random);
        }

        /// <summary>
        /// pushes the speed of adding and removing shapes to the limit. Usefull for causing vector errors
        /// </summary>
        public static void fastRemoval()
        {
            can = new GLCanvas(800, 600, TitleDetails: true, VSync: false); //good to test with vsync both off and on
            can.simpleBackBuffer = true;

            Shape t = can.AddCenteredText("Epilepsy warning!", 70f);
            Thread.Sleep(2000);
            can.Remove(t);

            int delay = 5;
            for (int i = 0; i < 10000; i++)
            {
                Polygon A = can.AddCenteredEllipse(200,300, 300, 300, Color.Random);
                Thread.Sleep(delay);
                can.Remove(A);
                Polygon B = can.AddCenteredEllipse(600, 300, 300, 300, Color.Random);
                Thread.Sleep(delay);
                can.Remove(B);
            }
        }

        public static void imageLoadingAbuse(string filepath)
        {
            can = new GLCanvas(800, 800, TitleDetails: true, VSync: false);
            can.simpleBackBuffer = true;

            int delay = 1000;

            Sprite s = new Sprite(filepath, can.Centre, new vec2(800, 800));  //can.AddCenteredSprite(filepath, 400, 400, 800, 800);

            for (int i = 0; i < 100; i++)
            {
                can.Add(s);
                Thread.Sleep(delay);
                can.Remove(s);
                Thread.Sleep(delay);
            }
        }

        private static Polygon[] Xe = new Polygon[26];
        private static Polygon[] Ye = new Polygon[20];
        public static void backBufferShapes()
        {
            can = new GLCanvas(1300, 900, TitleDetails: true, VSync: false);
            for (int i = 0; i < 1300; i++)
            {
                for (int j = 0; j < 900; j++)
                {
                    can.SetBBPixel(i, j, Color.Random);
                }
            }

            for (int i = 0; i < Ye.Length; i++)
                Ye[i] = new Polygon(new vec2(-50, 50 * i), new vec2(51), 0, 1,Color.Rainbow);
            for (int i = 0; i < Xe.Length; i++)
                Xe[i] = new Polygon(new vec2(50 * i, 0), new vec2(51), 0, 1, Color.Rainbow);
            can.Update += Can_Update;
        }

        private static void Can_Update()
        {
            vec2 disp = new vec2((float)Math.Sin(can.Time) * 2f, (float)(Math.Cos(can.Time * 2f) * 1.5f)) + new vec2(0.6f, 0.4f);
            disp *= 30 * can.DeltaTime;
            for (int i = 0; i < Ye.Length; i++)
            {
                Ye[i].Position += disp;
                can.SetBBShape(Ye[i]);
            }


            for (int i = 0; i < Xe.Length; i++)
            {
                Xe[i].Position += disp;
                can.SetBBShape(Xe[i]);
            }
        }
    }
}
