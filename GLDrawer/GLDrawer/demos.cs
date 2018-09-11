using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GLDrawer;

namespace Demos
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
            for (int i = 0; i < 1200; i++)
                for (int j = 0; j < 6; j++)
                    can.AddCenteredEllipse(i, 50 + j * 100, 100, 100, Color.Random);
        }

        /// <summary>
        /// pushes the speed of adding and removing shapes to the limit. Usefull for causing vector errors
        /// </summary>
        public static void fastRemoval()
        {
            can = new GLCanvas(800, 600, TitleDetails: true, VSync: false);

            Shape t = can.Add(new Text(new vec2(40, 300), 70, "Epilepsy Warning!", Color.White));
            Thread.Sleep(2000);
            can.RemoveShape(t);

            int delay = 10;
            for (int i = 0; i < 10000; i++)
            {
                Ellipse A = can.AddCenteredEllipse(200,300, 300, 300, Color.Random);
                Thread.Sleep(delay);
                can.RemoveShape(A);
                Ellipse B = can.AddCenteredEllipse(600, 300, 300, 300, Color.Random);
                Thread.Sleep(delay);
                can.RemoveShape(B);
            }
        }

        public static void imageLoadingAbuse(string filepath)
        {
            can = new GLCanvas(800, 800, TitleDetails: true, VSync: false);
            int delay = 10000;

            Sprite s = new Sprite(filepath, can.Centre, new vec2(800, 800));  //can.AddCenteredSprite(filepath, 400, 400, 800, 800);

            for (int i = 0; i < 100; i++)
            {
                can.Add(s);
                Thread.Sleep(delay);
                can.RemoveShape(s);
                Thread.Sleep(delay);
            }
        }
    }
}
