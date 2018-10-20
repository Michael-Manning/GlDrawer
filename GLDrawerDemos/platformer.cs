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
        public static float tileScale = 130;

        public static void run()
        {
           // GLDrawerDemos.levelEditorProgram.run();

            can = new GLCanvas(1200, 800, BackColor: new Color(66, 223, 244));
            loadLevel();
            can.Gravity = new vec2(0, -11);
            //can.Add(new sky());
            can.AddCenteredText("test", 40, Color.White);
            can.Add(new player());
            can.MouseScrolled += (i, c) => c.CamerZoom += i * 0.01f;
        }
        public static void loadLevel()
        {
            vec2 offset = new vec2(-5, -5);
            GLDrawerDemos.TileMap tilemap = new GLDrawerDemos.TileMap(GLDrawerDemos.levelEditor.workFile);
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    vec2 vec = tilemap.OpCollision[i, j];
                    if (vec != vec2.Zero)
                    can.Instantiate( new Tile(new vec2(tileScale * vec.x, tileScale * vec.y)), new vec2(i + offset.x + vec.x / 2f, -j + offset.y  - vec.y / 2f +tilemap.Ytiles ) * tileScale);
                }
            }
            for (int j = 0; j < tilemap.Ytiles; j++)
            {
                for (int i = 0; i < tilemap.Xtiles; i++)
                {
                    if(tilemap.SpriteGrid[i, j] != 0)
                    can.Instantiate(new TempTile(tilemap.SpritePaths[tilemap.SpriteGrid[i, j]]), new vec2((i + (int)offset.x) + 1f / 2f, ((j+ (int)offset.y) + 1f / 2f)  ) * tileScale);
                }
            }
        }
    }
    public class player : GameObject
    {
        float moveSpeed = 20;
        float airsPeed = 13;
        float jumpForce = 400;
        bool onGround = false;
        int onWall =  0;
        float maxSpeed = 9;
        Sprite guy;

        public override void Start()
        {
            transform.Position = new vec2(0, 100);
            guy = AddChildShape(new Sprite("../../../data/images/guy.png", vec2.Zero, new vec2(90, 90))) as Sprite;
         //   SetCircleCollider(90);
            rigidbody = new Rigidbody(this, 0.3f);
            rigidbody.SetFixedRotation(true);
        }
        public override void Update()
        {
            //left and right movement
            if (Canvas.GetSpecialKey(SpecialKeys.LEFT) && rigidbody.Velocity.x > -maxSpeed)
                rigidbody.AddForce(new vec2(onGround ? -moveSpeed : -airsPeed , 0));
            else if (Canvas.GetSpecialKey(SpecialKeys.RIGHT) && rigidbody.Velocity.x < maxSpeed)
                rigidbody.AddForce(new vec2(onGround ? moveSpeed : airsPeed, 0));
            else if( onGround)
            {
                int dir = rigidbody.Velocity.x > 0 ? 1 : -1;
                rigidbody.AddForce(new vec2(-rigidbody.Velocity.x * 3, 0));

            }

            //jumping off ground and walls
            if (Canvas.GetSpecialKeyDown(SpecialKeys.UP)){
                if (onGround)
                    rigidbody.AddForce(new vec2(0, jumpForce)); 
                else if (onWall != 0)
                {
                    rigidbody.Velocity = new vec2(rigidbody.Velocity.x, 0);
                    rigidbody.AddForce(new vec2(jumpForce * 0.8f * -onWall, jumpForce * 1.2f));
                }

            }
            if((!Canvas.GetSpecialKey(SpecialKeys.UP) || rigidbody.Velocity.y < 0) && !onGround && onWall == 0 && rigidbody.Velocity.y > -2)
                rigidbody.AddForce(new vec2(0, -25));
            //ground detection
            onGround = (Canvas.RayCast(transform.Position, transform.Position - new vec2(-46, 47)) ||
                        Canvas.RayCast(transform.Position, transform.Position - new vec2(46, 47)));
            //wall detection
            if (Canvas.RayCast(transform.Position, transform.Position + new vec2(47, 45)) ||Canvas.RayCast(transform.Position, transform.Position + new vec2(47, -45)))
                onWall = 1;
            else if(Canvas.RayCast(transform.Position, transform.Position + new vec2(-47, 45)) || Canvas.RayCast(transform.Position, transform.Position + new vec2(-47, -45)))
                onWall = -1;
            else 
                onWall = 0;
            if(onWall !=0)
                rigidbody.AddForce(new vec2(onWall * 8.0f, 0));
            guy.Tint = onGround ? new Color(0, 255, 0, 100) : onWall != 0 ? new Color(0, 0, 255, 100) : Color.Invisible;
            //camera movement
            if (Canvas.GetKeyDown('r') || transform.Position.y < -800)
                transform.Position = new vec2(0, 100);
            Canvas.CameraPosition = vec2.Lerp(Canvas.CameraPosition, transform.Position + new vec2(0, 100), 0.8f);
        }
    }
    public class Tile : GameObject
    {
        public vec2 scale;
        public Tile(vec2 s) => scale = s;
        public override void Start()
        {
            //AddChildShape(new Sprite("../../../data/images/wood.png", vec2.Zero, scale, 0, Color.Invisible, scale / platformer.tileScale));
            SetBoxCollider(scale);  
            rigidbody = new Rigidbody(this, 0.5f, kinematic: true);
        }
    }
    public class TempTile : GameObject
    {
        public string path;
        public TempTile(string p) {  
            path = p;
        }
        public override void Start()
        {
            AddChildShape(new Sprite(path, vec2.Zero, new vec2(platformer.tileScale)));
        }
    }

    public class sky : GameObject
    {
        public override void Start()
        {
            DrawIndex = 5;
            AddChildShape(new Sprite("../../../data/images/sky.jpg", vec2.Zero, new vec2(1202, 802)));
        }
        public override void LateUpdate() => transform.Position = new vec2(0) + Canvas.CameraPosition;
    }
}
