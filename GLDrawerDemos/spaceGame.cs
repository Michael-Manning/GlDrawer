using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawer;

namespace spaceGame
{
    public static class spaceGame
    {
        static GLCanvas can;
        static Player player;
        static Random rnd = new Random();
        public static List<enemySmall> enemies = new List<enemySmall>();
        public static Text co;
        public static void run()
        {
            can = new GLCanvas(800, 1100);
            can.Gravity = vec2.Zero;
            can.LoadAssets(new string[] {
                "../../../data/images/spaceship.png",
                "../../../data/images/enemy1.png",
                "../../../data/images/Fire_Spritesheet_Small.png"
                }, true);

            //background particle effect
            can.Add(new stars()
            {
                size = 2,
                count = 1200,
                speed = 140,
                movement = -1f,
                index = 8
            });
            can.Add(new stars()
            {
                size = 3,
                count = 400,
                speed = 200,
                movement = -0.4f,
                index = 7
            });

            player = new Player();
            can.Add(player);

            can.InvokeRepeating(delegate
            {
                can.Instantiate(new enemySmall(), new vec2((float)((rnd.NextDouble() - 0.5f) * (can.Width - 200)), 600));
            }, 1.0f);
            Console.ReadKey(); 
        }
    }
    public class Player : GameObject
    {
        float shootSpeed = 0.25f;
        float shotTimer = 0;
        public override void Start()
        {
            DrawIndex = 0;
           AddChildShape(new Sprite("../../../data/images/spaceship.png", vec2.Zero, new vec2(80)));
           Instantiate(new rocketFire(), new vec2(0, -40), -3.1459f / 2.0f, this);
        }
        public override void Update()
        {
            transform.Position = Canvas.MousePositionWorldSpace;
            Canvas.Camera = transform.Position / 6f;

            if (Canvas.GetMouse(0))
            {
                if(shotTimer >= shootSpeed)
                {
                    Instantiate(new laser(), transform.Position + new vec2(0, 20));
                    shotTimer = 0;
                }

            }
            shotTimer += DeltaTime;
        }
    }
    public class enemySmall : GameObject
    {
        int health = 3;
        Sprite shipRef;
        float myTime;
        public override void Start()
        {
            DrawIndex = 1;
            myTime = Time;
            
            shipRef = AddChildShape(new Sprite("../../../data/images/enemy1.png", vec2.Zero, new vec2(80),angle: 3.1459f)) as Sprite;
            Instantiate(new rocketFire(), new vec2(0, +20), 3.1459f / 2.0f, this);
            spaceGame.enemies.Add(this);
        }
        public override void Update()
        {
            transform.Position += new vec2((float)Math.Sin((myTime+ Time) * 2) * 3f, - 6.5f);
            if (transform.Position.y < -600)
            {
                spaceGame.enemies.Remove(this);
                Destroy();
            }
        }
        public void hit()
        {
            health--;
            if(health <= 0)
            {
                Instantiate(new explosion(), transform.Position);
                spaceGame.enemies.Remove(this);
                Destroy();
                return;
            }
            if (shipRef != null)
            {
                shipRef.Tint = new Color(255, 0, 0, 200);
                Canvas.Invoke(delegate { shipRef.Tint = Color.Invisible; }, 0.1f);
            }
            else
                Console.WriteLine("null sprite?");
        }
    }
    public class laser : GameObject
    {
        public override void Start()
        {
            DrawIndex = 2;
            AddChildShape( new Polygon(vec2.Zero, new vec2(7, 30), 0, 4, new Color(200,200,255)));
            Destroy(2);
        }
        public override void Update()
        {
            transform.Position += new vec2(0, 30);
            List<enemySmall> list = spaceGame.enemies.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                enemySmall e = list[i];
                if (e.transform.Position.Length(transform.Position) < 100)
                {
                    e.hit();
                    Destroy();
                }
            }
        }
    }
    public class rocketFire : GameObject
    {
        public override void Start()
        {
            DrawIndex = 2;
            ParticleSystem fire =  new ParticleSystem(80, 0.6f, "../../../data/images/Fire_Spritesheet_Small.png", 8);
            fire.Spread = 0.5f;
            fire.Continuous = true;
            fire.BurstMode = false;
            fire.StartSize = 10;
            fire.EndSize = 10;
            fire.Speed = 550.0f;
            fire.SpeedPrecision = 30.0f;
            fire.LifePrecision = 0.4f;
            fire.Radius = 50;
            fire.RelitivePosition = true;
            AddChildShape(fire);
        }
    }
    public class explosion : GameObject
    {
        public override void Start()
        {
            DrawIndex = 2;
            ParticleSystem fire = new ParticleSystem(7, 1.0f, "../../../data/images/Fire_Spritesheet_Small.png", 8);
            fire.Angle = 2;
            fire.Spread = 3.14595f * 2f;
            fire.Continuous = false;
            fire.BurstMode = true;
            fire.StartSize = 35;
            fire.EndSize = 20;
            fire.Speed = 200.0f;
            fire.SpeedPrecision = 30.0f;
            fire.LifePrecision = 0.2f;
            fire.Radius = 50;
            fire.RelitivePosition = true;
            AddChildShape(fire);
            Destroy(3);
        }
    }
    public class stars : GameObject
    {
        public float size, speed, movement;
        public int count, index;
        public override void Start()
        {
            ParticleSystem stars;
            if (size < 3)
                stars = new ParticleSystem(count, 13);
            else
                stars = new ParticleSystem(count, 13, "../../../data/images/star.png");
            DrawIndex = index;
            transform.Rotation = -3.1459f / 2.0f; // stars.Angle = -3.1459f / 2.0f;
            stars.Continuous = true;
            stars.StartSize = size;
            stars.EndSize = size;
            stars.Speed = speed;
            stars.Radius = 840;
            stars.StartColor = Color.White;
            stars.EndColor = Color.White;
            stars.RelitivePosition = false;
            AddChildShape(stars);
        }
        public override void Update()
        {
            transform.Position = new vec2(0, 700) - Canvas.Camera * movement;
        }
    }
}
