using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GLDrawer;

namespace platformGame
{
    public static class AdvancedPlatformer
    {
        static GLCanvas can;
        static Random rnd = new Random();
        public static float tileScale = 130;
        static List<Wall> Walls = new List<Wall>();
        static List<Texture> Textures = new List<Texture>();

        public static void run()
        {
            can = new GLCanvas(1200, 800, BackColor: new Color(66, 223, 244));
            loadLevel();
            can.Gravity = new vec2(0, -11);
            can.Add(new AdvancedPlayer());
            can.MouseScrolled += Can_MouseScrolled;
        }

        private static void Can_MouseScrolled(int Delta, GLCanvas Canvas)
        {
            can.CamerZoom += Delta * 0.01f;
            if (can.CamerZoom < 0.1f)
                can.CamerZoom = 0.1f;
        }

        public static void loadLevel()
        {
            Walls.ForEach(w => w.Destroy());
            Textures.ForEach(t => t.Destroy());

            vec2 offset = new vec2(-5, -5);
            GLDrawerDemos.TileMap tilemap = new GLDrawerDemos.TileMap(GLDrawerDemos.levelEditor.workFile);

            foreach (GLDrawerDemos.TileMap.Tile t in tilemap.OpCollision)
                Walls.Add(can.Instantiate(new Wall(new vec2(tileScale * t.w, tileScale * t.h)), new vec2(t.x + offset.x + t.w / 2f, -t.y + offset.y - t.h / 2f + tilemap.Ytiles) * tileScale) as Wall);

            for (int l = 0; l < tilemap.layers; l++)
                for (int j = 0; j < tilemap.Ytiles; j++)
                    for (int i = 0; i < tilemap.Xtiles; i++)
                        if (tilemap.SpriteGrid[l][i, j] != 0)
                            Textures.Add(can.Instantiate(new Texture(tilemap.SpritePaths[tilemap.SpriteGrid[l][i, j]], l), new vec2((i + (int)offset.x) + 1f / 2f, ((j + (int)offset.y) + 1f / 2f)) * tileScale) as Texture);
        }
    }
    public class AdvancedPlayer : GameObject
    {
        float moveSpeed = 20;
        float airsPeed = 13;
        float jumpForce = 400;
        bool onGround = false;
        int onWall = 0;
        float maxGroundSpeed = 14;
        float maxAirSpeed = 9;
        Sprite guy;

        public override void Start()
        {
            DrawIndex = -1;
            guy = AddChildShape(new Sprite("../../../data/images/guy.png", vec2.Zero, new vec2(90, 90))) as Sprite;
            rigidbody = new Rigidbody(this, 0.3f);
            rigidbody.SetFixedRotation(true);
        }
        public override void Update()
        {
            //left and right movement
            if (Canvas.GetSpecialKey(SpecialKeys.LEFT) && rigidbody.Velocity.x > onGround ? -maxGroundSpeed : -maxAirSpeed)
                rigidbody.AddForce(new vec2(onGround ? -moveSpeed : -airsPeed, 0));

            else if (Canvas.GetSpecialKey(SpecialKeys.RIGHT) && rigidbody.Velocity.x < onGround ? maxGroundSpeed : maxAirSpeed)
                rigidbody.AddForce(new vec2(onGround ? moveSpeed : airsPeed, 0));

            //exerise finer control over deacceleration on the ground
            else if (onGround)
                rigidbody.AddForce(new vec2(-rigidbody.Velocity.x * 3, 0));


            //jumping off ground and walls
            if (Canvas.GetSpecialKeyDown(SpecialKeys.UP))
            {
                if (onGround)
                    rigidbody.AddForce(new vec2(0, jumpForce));
                else if (onWall != 0)
                {
                    rigidbody.Velocity = new vec2(rigidbody.Velocity.x, 0);
                    rigidbody.AddForce(new vec2(jumpForce * 0.7f * -onWall, jumpForce * 1.2f));
                }
            }

            //allows for variable jump height based on how long the jump button is held down
            if ((!Canvas.GetSpecialKey(SpecialKeys.UP) || rigidbody.Velocity.y < 0) && !onGround && onWall == 0 && rigidbody.Velocity.y > -2)
                rigidbody.AddForce(new vec2(0, -25));

            //ground detection via raycast
            onGround = (Canvas.RayCast(transform.Position, transform.Position - new vec2(-46, 47)) ||
                        Canvas.RayCast(transform.Position, transform.Position - new vec2(46, 47)));

            //wall detection via raycast
            if (Canvas.RayCast(transform.Position, transform.Position + new vec2(47, 45)) || Canvas.RayCast(transform.Position, transform.Position + new vec2(47, -45)))
                onWall = 1;
            else if (Canvas.RayCast(transform.Position, transform.Position + new vec2(-47, 45)) || Canvas.RayCast(transform.Position, transform.Position + new vec2(-47, -45)))
                onWall = -1;
            else
                onWall = 0;

            //add slight magnetism to walls to prevent bouncing off and increase friction
            if (onWall != 0)
                rigidbody.AddForce(new vec2(onWall * 8.0f, 0));

            //color indicator for player state
            guy.Tint = onGround ? new Color(0, 255, 0, 100) : onWall != 0 ? new Color(0, 0, 255, 100) : Color.Invisible;

            //reset position
            if (Canvas.GetKeyDown('r') || transform.Position.y < -800)
                transform.Position = new vec2(0, 100);

            //camera movement
            Canvas.CameraPosition = vec2.Lerp(Canvas.CameraPosition, transform.Position + new vec2(0, 100), 0.8f);
        }
    }
    public class Wall : GameObject
    {
        public vec2 scale;
        public Wall(vec2 s) => scale = s;
        public override void Start()
        {
            //AddChildShape(new Sprite("../../../data/images/wood.png", vec2.Zero, scale, 0, Color.Invisible, scale / platformer.tileScale));
            SetBoxCollider(scale);
            rigidbody = new Rigidbody(this, 0.5f, kinematic: true);
        }
    }
    public class Texture : GameObject
    {
        public string path;
        int index;
        public Texture(string p, int Index)
        {
            path = p;
            index = Index;
        }
        public override void Start()
        {
            DrawIndex = index;
            AddChildShape(new Sprite(path, vec2.Zero, new vec2(AdvancedPlatformer.tileScale)));
        }
    }
}
