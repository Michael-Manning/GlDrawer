using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawer;

namespace SimplePhysics
{
    class Program
    {
        static void Main(string[] args)
        {
            GLCanvas can = new GLCanvas(1000, 1000, BackColor: new Color(50), LegacyCoordinates: false);
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
            Console.ReadKey();
        }
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
}
