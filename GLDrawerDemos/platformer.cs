using System;
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
            loadLevel();
            can.Gravity = new vec2(0, -11);
            can.Add(new sky());
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
                    if (blocks[j] == "1")
                        can.Instantiate(new Tile(), new vec2((j + offset.x) * tileScale, (-i + 16 + offset.y) * tileScale));
            }
        }
    }
    public class player : GameObject
    {
        float moveSpeed = 13;
        float jumpForce = 260;
        bool onGround = false;
        int onWall =  0;
        float maxSpeed = 7;
        Sprite guy;

        public override void Start()
        {
            transform.Position = new vec2(0, 100);
            guy = AddChildShape(new Sprite("../../../data/images/guy.png", vec2.Zero, new vec2(90, 90))) as Sprite;
            SetCircleCollider(90);
            rigidbody = new Rigidbody(this, 0.5f);
            rigidbody.AddForce(new vec2(100, 0));
            rigidbody.SetFixedRotation(true);
        }
        public override void Update()
        {
            if (Canvas.GetSpecialKey(SpecialKeys.LEFT) && rigidbody.Velocity.x > -maxSpeed)
            {
                rigidbody.AddForce(new vec2(-moveSpeed, 0));
            }
            if (Canvas.GetSpecialKey(SpecialKeys.RIGHT) && rigidbody.Velocity.x < maxSpeed){
                rigidbody.AddForce(new vec2(moveSpeed, 0));
            }
            if (Canvas.GetSpecialKeyDown(SpecialKeys.UP)){
                if (onGround)
                    rigidbody.AddForce(new vec2(0, jumpForce));
                else if (onWall != 0)
                    rigidbody.AddForce(new vec2(jumpForce * 0.8f * -onWall, jumpForce * 1.2f));
            }
            if((!Canvas.GetSpecialKey(SpecialKeys.UP) || rigidbody.Velocity.y < 0) && !onGround && onWall == 0 && rigidbody.Velocity.y > -2)
                rigidbody.AddForce(new vec2(0, -25));

            onGround = (Canvas.RayCast(transform.Position, transform.Position - new vec2(-45, 47)) ||
                        Canvas.RayCast(transform.Position, transform.Position - new vec2(45, 47)));

            if (Canvas.RayCast(transform.Position, transform.Position + new vec2(47, 45)))
                onWall = 1;
            else if(Canvas.RayCast(transform.Position, transform.Position + new vec2(-47, 45)))
                onWall = -1;
            else 
                onWall = 0;
            if(onWall !=0)
                rigidbody.AddForce(new vec2(onWall * 8.0f, 0));
            guy.Tint = onGround ? new Color(0, 255, 0, 100) : onWall != 0 ? new Color(0, 0, 255, 100) : Color.Invisible;
        
            if (transform.Position.y < -400)
                transform.Position = new vec2(0, 100);
            Canvas.Camera = vec2.Lerp(Canvas.Camera, transform.Position + new vec2(0, 100), 0.8f);
        }
    }
    public class Tile : GameObject
    {
        public override void Start()
        {
            AddChildShape(new Sprite("../../../data/images/wood.png", vec2.Zero, new vec2(platformer.tileScale)));
            rigidbody = new Rigidbody(this, 0.5f, kinematic: true);
        }
    }
    public class sky : GameObject
    {
        public override void Start()
        {
            DrawIndex = 5;
            AddChildShape(new Sprite("../../../data/images/sky.jpg", vec2.Zero, new vec2(1202, 802)));
        }
        public override void LateUpdate() => transform.Position = new vec2(0) + Canvas.Camera;
    }
}
