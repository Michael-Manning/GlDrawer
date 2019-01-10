using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using GLDrawer;

namespace GLDrawerDemos
{
    static class demos
    {
        public static GLCanvas can;
        public static GLCanvas previewCan;

        /// <summary>
        /// displays to rotating shapes and turns them red when moused over
        /// </summary>
        public static void IntersectTest()
        {
            //create a canvas and two polygons 
            can = new GLCanvas();
            Polygon rect = can.AddCenteredRectangle(-150, 0, 200, 100, Color.White, Angle: 1);
            Polygon circ = can.AddCenteredEllipse(150, 0, 150, 150, Color.White);

            //runs checks every frame
            can.Update += delegate
            {
                rect.Angle += 0.005f;
                if (rect.Intersect(can.MousePosition))
                    rect.FillColor = Color.Red;
                else
                    rect.FillColor = Color.White;

                if (circ.Intersect(can.MousePosition))
                    circ.FillColor = Color.Red;
                else
                    circ.FillColor = Color.White;
            };
        }

        /// <summary>
        /// a simple testbed in which you can create shapes with physics
        /// </summary>
        public static void PhysicsTest()
        {
            can = new GLCanvas(1000, 1000, BackColor: new Color(50));
            can.AddCenteredText("Click and drag to fling boxes and balls.", 30, new Color(255, 160));

            can.Instantiate(new wall(), new vec2(1000, 0));
            can.Instantiate(new wall(), new vec2(-1000, 0));
            can.Instantiate(new wall(), new vec2(0, -1000));

            vec2 A = vec2.Zero, B = vec2.Zero;
            Line traj = can.Add(new Line(new vec2(), new vec2(), 8, Color.White)) as Line;
            traj.Hidden = true;

            can.Update += delegate
            {
                traj.End = can.MousePosition;
                if (can.GetMouseDown(0))
                {
                    A = can.MousePosition;
                    traj.Start = A;
                    traj.Hidden = false;
                }
                else if (can.GetMouseUp(0))
                {
                    traj.Hidden = true;
                    B = can.MousePosition;
                    can.Instantiate(new box(A - B, 4), A);
                }
                else if (can.GetMouseDown(1))
                {
                    A = can.MousePosition;
                    traj.Start = A;
                    traj.Hidden = false;
                }
                else if (can.GetMouseUp(1))
                {
                    traj.Hidden = true;
                    B = can.MousePosition;
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
                Polygon A = can.AddCenteredEllipse(-200, 0, 300, 300, Color.Random);
                Thread.Sleep(delay);
                can.Remove(A);
                Polygon B = can.AddCenteredEllipse(300, 0, 300, 300, Color.Random);
                Thread.Sleep(delay);
                can.Remove(B);
            }
        }

        /// <summary>
        /// Loads and deloads an image very fast to check for memory leaks and crashes
        /// </summary>
        public static void ImageLoadingAbuse(string filepath)
        {
            can = new GLCanvas(800, 800, TitleDetails: true, VSync: false);

            int delay = 1000;

            Sprite s = new Sprite(filepath, can.Center, new vec2(800, 800));  //can.AddCenteredSprite(filepath, 400, 400, 800, 800);

            for (int i = 0; i < 100; i++)
            {
                can.Add(s);
                Thread.Sleep(delay);
                can.Remove(s);
                Thread.Sleep(delay);
            }
        }

        public static GLMouseEvent mouseClickCallback;
        public static GLMouseEvent mouseMovedCallback;
        public static void FormTest()
        {
            Application.EnableVisualStyles();
            TestForm tform = new TestForm();

            can = new GLCanvas(tform, tform.surface, BackColor: Color.LightGray, debugMode: true);
            previewCan = new GLCanvas(tform, tform.preview, BackColor: tform.BackColor);

            tform.updatePreview();
            can.MouseLeftClick += Can_MouseLeftClick;
            can.MouseMove += Can_MouseMove;
            Application.Run(tform);
        }
        private static void Can_MouseMove(vec2 Position, GLCanvas Canvas)
        {
            mouseMovedCallback.Invoke(Position, Canvas);
        }

        private static void Can_MouseLeftClick(vec2 Position, GLCanvas Canvas)
        {
            mouseClickCallback.Invoke(Position, Canvas);
        }

        private static Polygon[] Xe = new Polygon[26];
        private static Polygon[] Ye = new Polygon[20];
        /// <summary>
        /// Sets individual pixels to the back buffer as well as shapes
        /// </summary>
        public static void BackBufferTest()
        {
            can = new GLCanvas(1300, 900, TitleDetails: true, VSync: true);
            can.CameraPosition = can.Center;
            Color.setRandomSeed(123);
            float time = System.Environment.TickCount;
            for (int l = 0; l < 1; l++)
            {
                for (int i = 0; i < 1300; i++)
                {
                    for (int j = 0; j < 900; j++)
                    {
                        can.SetBBPixelFast(i, j, Color.Random);
                    }
                }
            }

            time = System.Environment.TickCount - time;
            Console.WriteLine("Filled in " + time + " milliseconds");

            for (int i = 0; i < Ye.Length; i++)
                Ye[i] = new Polygon(new vec2(-50, 50 * i), new vec2(51), 0, 1, Color.Rainbow);
            for (int i = 0; i < Xe.Length; i++)
                Xe[i] = new Polygon(new vec2(50 * i, 0), new vec2(51), 0, 1, Color.Rainbow);
            can.Update += Can_Update;
        }

        /// <summary>
        /// Tests color blending when using multiple threads with the back buffer
        /// </summary>
        public static void BackBufferMultithread()
        {
            can = new GLCanvas(1300, 900, TitleDetails: true, VSync: true);
            can.CameraPosition = can.Center;

            Thread A = new Thread(new ThreadStart(delegate { fillThread(true); }));
            Thread B = new Thread(new ThreadStart(delegate { fillThread(false); }));

            A.Start();
            B.Start();

            A.Join();
            B.Join();
        }
        private static void fillThread(bool red)
        {
            for (int l = 0; l < 1; l++)
            {
                for (int i = 0; i < 1300; i++)
                {
                    for (int j = 0; j < 900; j++)
                    {
                        if (red)
                            can.SetBBPixelFast(i, j, new Color(255, 0, 0, 70));
                        else
                            can.SetBBPixelFast(i, j, Color.Random);
                    }
                    if (red)
                        Thread.Sleep(3);
                    else
                        Thread.Sleep(1);
                }
            }
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
