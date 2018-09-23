using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLDrawer
{
    public abstract class GameObject
    {
        public Transform transform;
        public Shape shape;

        public GameObject(vec2 ? Location = null)
        {
            if (Location != null)
                transform = new Transform((vec2)Location);
            else
                transform = new Transform();
        }
        public GameObject(Transform transform)
        {
            this.transform = transform;
        }

        internal  void internalUpdate()
        {

        }

        public virtual void Start() { }

        public virtual void Update() { }
    }
    /// <summary>
    ///represents a gameObject's location in world space
    /// </summary>
    /// <param>name= "position" location in world space</param>
    /// <param name="velocity">Gets multiplied by DeltaTime and added to the position every frame</param>
    /// <param name="maxAngularVelocity">Speed cap of the transform at any angle. OVERRIDDIN BY RIGIDBODIES </param>
    public class Transform
    {
        public vec2 position;
        public vec2 velocity;
        public float? maxAngularVelocity = null;
        public float rotation;

        public Transform()
        {
            position = vec2.Zero; 
            rotation = 0;
        }
        public Transform(vec2 Position, float Rotation = 0)
        {
            position = Position;
            rotation = Rotation;
        }
    }
}
