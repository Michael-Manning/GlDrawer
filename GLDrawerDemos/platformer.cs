using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawer;

namespace platformGame
{
    public static class SimplePlatformer
    {
        static GLCanvas can;
        public static float tileScale = 130;
        const string levelPath = "../../../data/other/Simple Level.txt";

        public static void run()
        {
            can = new GLCanvas(1200, 800, BackColor: new Color(66, 223, 244));
            loadLevel();
            can.Gravity = new vec2(0, -10);
            can.Add(new Player());
            can.CameraZoom = 0.6f;
        }
        public static void loadLevel()
        {
            //load the level layout from the data folder and place tiles
            string[] lines = System.IO.File.ReadAllLines(levelPath);
            for (int j = 0; j < 16; j++)
            {
                int[] tiles = Array.ConvertAll(lines[j].Split(' '), int.Parse);
                for (int i = 0; i < 20; i++)
                    if (tiles[i] == 1)
                        can.Instantiate(new Tile(), new vec2(i * tileScale, (-j + 16) * tileScale));
            }
        }
    }
    public class Player : GameObject
    {
        //movement settings
        float moveSpeed = 14f;
        float jumpForce = 250f;
        float maxSpeed = 9;

        Sprite guy;
        bool onGround = false;

        public override void Start()
        {
            //attach a sprite, a rigidbody, and set the starting position
            guy = AddChildShape(new Sprite("../../../data/images/guy.png", vec2.Zero, new vec2(90, 90))) as Sprite;
            SetCircleCollider(90);
            SetDefaultRigidbody();
            rigidbody.SetFixedRotation(true);
            transform.Position = new vec2(0, 350);
        }
        public override void Update()
        {
            //left and right movement
            if (Canvas.GetSpecialKey(SpecialKeys.LEFT) && rigidbody.Velocity.x > -maxSpeed)
                rigidbody.AddForce(new vec2(-moveSpeed, 0));
            else if (Canvas.GetSpecialKey(SpecialKeys.RIGHT) && rigidbody.Velocity.x < maxSpeed)
                rigidbody.AddForce(new vec2(moveSpeed, 0));

            //jumping
            if (Canvas.GetSpecialKeyDown(SpecialKeys.UP) && onGround)
            {
                rigidbody.AddForce(new vec2(0, jumpForce));
                onGround = false;
            }                   
                      
            //reset player position
            if (Canvas.GetKeyDown('r') || transform.Position.y < -800)
                transform.Position = new vec2(0, 350);

            //smooth camera movement
            Canvas.CameraPosition = vec2.Lerp(Canvas.CameraPosition, transform.Position + new vec2(0, 100), 0.8f);
        }
        public override void OnCollisionEnter(Collision col)
        {
            //when the player touches something it can jump again
            onGround = true;
        }
    }
    public class Tile : GameObject
    {
        public override void Start()
        {
            AddChildShape(new Polygon(vec2.Zero, new vec2(SimplePlatformer.tileScale), 0, 4, Color.Random + 70, 9, Color.Blue));
            rigidbody = new Rigidbody(this, 0.5f, kinematic: true);
        }
    }
}
