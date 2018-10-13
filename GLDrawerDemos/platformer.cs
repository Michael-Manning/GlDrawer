using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GLDrawer;

namespace platformGame
{
    public static class platformer
    {
        static GLCanvas can;
        static Random rnd = new Random();
        const string levelPath = @"C:\Users\Micha\Source\Repos\levelBuilder\levelBuilder\bin\level.txt";
        public static float tileScale = 100;

        public static void run()
        {
            can = new GLCanvas(1200, 800, BackColor: Color.LightBlue);
            //can.Instantiate(new level(), new vec2(0, -350));
            loadLevel();
            can.Add(new player());
        }

        public static void loadLevel()
        {
            vec2 offset = new vec2(-2, -2);


            string[] lines = File.ReadAllLines(levelPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] blocks = lines[i].Split(' ');
                for (int j = 0; j < blocks.Length; j++)
                {
                    if (blocks[j] == "1")
                        can.Instantiate(new Tile(), new vec2((j + offset.x) * tileScale, (-i + 16 + offset.y) * tileScale));
                }
            }
        }
    }
    public class player : GameObject
    {
        float moveSpeed = 30;
        float jumpForce = 400;

        public override void Start()
        {
            transform.Position = new vec2(0, 100);
            AddChildShape(new Polygon(vec2.Zero, new vec2(100, 100), 0, 4, Color.White));
            rigidbody = new Rigidbody(this, 0.2f);
            rigidbody.AddForce(new vec2(100, 0));
            rigidbody.SetFixedRotation(true);
        }
        public override void Update()
        {
            if (Canvas.GetSpecialKey(SpecialKeys.LEFT)){
                // rigidbody.Velocity = new vec2(-moveSpeed, rigidbody.Velocity.y);
                rigidbody.AddForce(new vec2(-moveSpeed, 0));
            }
            if (Canvas.GetSpecialKey(SpecialKeys.RIGHT)){
                // rigidbody.Velocity = new vec2(moveSpeed, rigidbody.Velocity.y);
                rigidbody.AddForce(new vec2(moveSpeed, 0));
            }
            if (Canvas.GetSpecialKeyDown(SpecialKeys.UP)){
                //   rigidbody.Velocity = new vec2(rigidbody.Velocity.x, jumpForce);
                rigidbody.AddForce(new vec2(0, jumpForce));
            }
            if (Canvas.GetSpecialKeyDown(SpecialKeys.DOWN))
            {

                Canvas.Camera += new vec2(0, -1);
            }

            Canvas.Camera = vec2.Lerp(Canvas.Camera, transform.Position + new vec2(0, 100), 0.7f);
        }
    }
    public class Tile : GameObject
    {
        public override void Start()
        {
            Shape s = AddChildShape(new Polygon(vec2.Zero, new vec2(platformer.tileScale), 0, 4, Color.Blue));
            rigidbody = new Rigidbody(this, 0.2f, kinematic: true);
        }
    }
}
