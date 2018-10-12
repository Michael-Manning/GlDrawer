using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;

namespace GLDrawer
{
    public class ParticleSystem : Shape
    {
       // int maxParticles { get => internalPS.maxParticles; set => internalPS.maxParticles = value; }
        public float StartSize { get => internalPS.startSize; set => internalPS.startSize = value; }
        public float EndSize { get => internalPS.endSize; set => internalPS.endSize = value; }
        public float LifeLength { get => internalPS.lifeLength; set => internalPS.lifeLength = value; }
        public float LifePrecision { get => internalPS.lifePrecision; set => internalPS.lifePrecision = value; }
        public float Spread { get => internalPS.spread; set => internalPS.spread = value; }
        public override float Angle { get => internalPS.angle; set => internalPS.angle = value; }
        public float Speed { get => internalPS.speed; set => internalPS.speed = value; }
        public float SpeedPrecision { get => internalPS.speedPrecision; set => internalPS.speedPrecision = value; }
        public float Radius { get => internalPS.radius; set => internalPS.radius = value; }
        public vec2 Gravity { get => new vec2(internalPS.gravity.x, internalPS.gravity.x); set => internalPS.gravity = new unmanaged_vec2(value.x, value.y); }
        public vec2 ExtraVelocity { get => new vec2(internalPS.extraVelocity.x, internalPS.extraVelocity.x); set => internalPS.extraVelocity = new unmanaged_vec2(value.x, value.y); }
        public Color StartColor { get => internalPS.startCol; set => internalPS.startCol = value; }
        public Color EndColor { get => internalPS.endCol; set => internalPS.endCol = value; }
        public bool RelitivePosition { get => internalPS.relitivePosition; set => internalPS.relitivePosition = value; }
        public bool Continuous { get => internalPS.continuous; set => internalPS.continuous = value; }
        public bool BurstMode { get => internalPS.burst; set => internalPS.burst = value; }

        internal unmanaged_PS internalPS;

        public ParticleSystem(int MaxParticles, float LifeLength, vec2? position = null)
        {
            vec2  pos = position == null ? vec2.Zero : (vec2)position;
            internalPS = new unmanaged_PS(MaxParticles, LifeLength);
            internalGO = new unmanaged_GO(internalPS, pos.x, pos.y, 0, 0, 0, 0);
        }
        public ParticleSystem(int MaxParticles, float LifeLength, string texturePath, vec2? position = null)
        {
            if (!System.IO.File.Exists(texturePath))
                throw new ArgumentException("image file was not found", "texturePath");

            vec2 pos = position == null ? vec2.Zero : (vec2)position;
            internalPS = new unmanaged_PS(MaxParticles, LifeLength);
            internalPS.setTexture(texturePath);
            internalGO = new unmanaged_GO(internalPS, pos.x, pos.y, 0, 0, 0, 0);
        }
        public ParticleSystem(int MaxParticles, float LifeLength, string texturePath, int tilesPerRow, vec2? position = null)
        {
            if (!System.IO.File.Exists(texturePath))
                throw new ArgumentException("image file was not found", "texturePath");
            System.Drawing.Image img = System.Drawing.Image.FromFile(texturePath);          
            if (img.Width != img.Height)
                throw new ArgumentException("Image not square");

            vec2 pos = position == null ? vec2.Zero : (vec2)position;
            internalPS = new unmanaged_PS(MaxParticles, LifeLength);
            internalPS.setAnimation(texturePath, img.Height, tilesPerRow);
            internalGO = new unmanaged_GO(internalPS, pos.x, pos.y, 0, 0, 0, 0);
            img.Dispose();
        }

        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Console.WriteLine("GC");
            internalGO.dispose();
            internalPS.dispose();
            GC.SuppressFinalize(this);
        }

        ~ParticleSystem()
        {
            Dispose();
        }
    }
}
