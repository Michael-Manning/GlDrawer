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

        public static void IntersectTest()
        {
            can = new GLCanvas();
            Polygon rect = can.AddCenteredRectangle(-150, 0, 200, 100, Color.White, Angle: 1);
            Polygon circ = can.AddCenteredEllipse(150, 0, 150, 150, Color.White);
            can.Update += delegate
            {
                if (rect.Intersect(can.MousePositionScreenSpace))
                    rect.FillColor = Color.Red;
                else
                    rect.FillColor = Color.White;

                if (circ.Intersect(can.MousePositionScreenSpace))
                    circ.FillColor = Color.Red;
                else
                    circ.FillColor = Color.White;
            };
        }

        public static void PhysicsTest()
        {
            can = new GLCanvas(1000,1000, BackColor: new Color(50));

            can.AddCenteredText("Click and drag to fling boxes and balls.", 30, new Color(255, 160));
            can.Instantiate(new wall(), new vec2(1000, 0));
            can.Instantiate(new wall(), new vec2(-1000, 0));
            can.Instantiate(new wall(), new vec2(0, -1000)); 

            vec2 A = vec2.Zero, B = vec2.Zero;
            Line traj = can.Add(new Line(new vec2(), new vec2(), 8, Color.White)) as Line;
            traj.Hidden = true;

            can.Update += delegate
            {
                traj.End = can.MousePositionScreenSpace;
                if (can.GetMouseDown(0))
                {
                    A = can.MousePositionScreenSpace;
                    traj.Start = A;
                    traj.Hidden = false;
                }          
                else if (can.GetMouseUp(0))
                {
                    traj.Hidden = true;
                    B = can.MousePositionScreenSpace;
                    can.Instantiate(new box(A - B, 4), A);             
                }
                else if (can.GetMouseDown(1))
                {
                    A = can.MousePositionScreenSpace;
                    traj.Start = A;
                    traj.Hidden = false;
                }
                else if (can.GetMouseUp(1))
                {
                    traj.Hidden = true;
                    B = can.MousePositionScreenSpace;
                    can.Instantiate(new box(A - B, 1), A);
                }
            };
        }
        class box : GameObject
        {
            Random rnd = new Random();
            vec2 vel = vec2.Zero;
            int type = 0;
            public box(vec2 V, int t)
            { 
                vel = V;
                type = t;
            }
            //static Random = new 
            public override void Start()
            {
                vec2 sc = new vec2(rnd.Next(30, 230), rnd.Next(70, 130));
                if (type == 1)
                    sc = new vec2(rnd.Next(30, 230));
                Shape box = AddChildShape(new Polygon(vec2.Zero, sc, 0, type, Color.Random, 5, Color.White));
                rigidbody = new Rigidbody(this);
                rigidbody.AddForce(vel);
                rigidbody.AddTorque(100);
            }
        }
        class wall : GameObject
        {
            public override void Start()
            {
                Shape s = AddChildShape(new Polygon(vec2.Zero, new vec2(1000, 1000), 0, 4, new Color(255, 50)));
                rigidbody = new Rigidbody(this, kinematic: true);
            }
        }

        /// <summary>
        /// adds 7200 shapes to the screen to test thread safety and performance
        /// </summary>
        public static void GroupAdd()
        {
            can = new GLCanvas(1200, 600, TitleDetails: true, VSync: true);

            for (int i = 0; i < 1200; i++)
                for (int j = 0; j < 6; j++)
                    can.AddCenteredEllipse(i, 50 + j * 100, 100, 100, Color.Random);
        }

        /// <summary>
        /// pushes the speed of adding and removing shapes to the limit. Usefull for causing vector errors
        /// </summary>
        public static void FastRemoval()
        {
            can = new GLCanvas(800, 600, TitleDetails: true, VSync: false); //good to test with vsync both off and on

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

        public static void ImageLoadingAbuse(string filepath)
        {
            can = new GLCanvas(800, 800, TitleDetails: true, VSync: false);

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
        public static void BackBufferShapes()
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
