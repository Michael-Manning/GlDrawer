using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLDrawerCLR;

namespace GLDrawer
{
    public class Rigidbody : IDisposable
    {
        private GameObject go;
        internal unmanaged_rigBody internalBody;

        public void AddForce(vec2 force) => internalBody.addForce(force.x, force.y);
        public void AddTorque(float torque) => internalBody.addTorque(torque);
        public bool Kinematic { get; private set; }
        public void SetFixedRotation(bool Fixed) => internalBody.lockRotation(Fixed);
        public string Tag { get; private set; }

        public vec2 Velocity
        {
            get
            {
                unmanaged_vec2 v = internalBody.getVelocity();
                return new vec2(v.x, v.y);
            }
            set
            {
                internalBody.setVelocity(value.x, value.y);
            }
        }

        public float AngularVelocity { get => internalBody.angularVelocity; set => internalBody.angularVelocity = value; }

        public Rigidbody(GameObject gameObject, float friction = 0.8f, bool kinematic = false, bool isSensor = false, string tag = "")
        {
            //need a gameobject to operate
            if (gameObject == null)
                throw new NullReferenceException("gameobject was null");
            
            //creates default collider if there isnt one
            if (gameObject.colliderType == - 1)
            {
                bool succsess = false;
                if(gameObject.shapeChildren.Count > 0)
                {
                    foreach(Shape s in gameObject.shapeChildren)
                    {
                        if(s != null)
                        {
                            gameObject.setColliderFromShape(s);
                            succsess = true;
                            break;
                        }
                    }
                }
                if(!succsess)
                    throw new NullReferenceException("collider was null and no child shapes were found");
            }
            Kinematic = kinematic;
            Tag = tag;
            if(tag == "")
                internalBody = new unmanaged_rigBody(gameObject.can.GLWrapper, gameObject.internalGO, gameObject.colliderType, friction, kinematic, isSensor);
            else
                internalBody = new unmanaged_rigBody(gameObject.can.GLWrapper, gameObject.internalGO, gameObject.colliderType, friction, kinematic, isSensor, tag);
        }

        internal void disable()
        {
            internalBody.disable();
        }

        private bool disposed;
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            internalBody.dispose();
            GC.SuppressFinalize(this);
        }

        ~Rigidbody()
        {
            Dispose();
        }
    }

    public struct Collision
    {
        public string Tag;
    }
    
    //public abstract class Collider
    //{
    //    protected GameObject go;

    //    public bool IsTrigger;
    //    // get intersecting gameobjects()
    //    public abstract bool Intersect(vec2 point);
    //    public static Collider FromShape(GameObject gameObject, Shape shape)
    //    {
    //        if (shape.isCircle)
    //            return new CircleCollider(gameObject, shape.Scale.x);
    //        return new BoxCollider(gameObject, shape.Scale);
    //    }

    //}
    //public class BoxCollider : Collider
    //{
    //    public vec2 Scale;
    //    public BoxCollider(GameObject gameObject, vec2 scale, bool isTrigger = false)
    //    {
    //        Scale = scale;
    //        IsTrigger = isTrigger;
    //        go = gameObject;
    //    }
    //    public override bool Intersect(vec2 point)
    //    {
    //        return unmanaged_Canvas.TestRect(go.transform.Position.x, go.transform.Position.y, Scale.x, Scale.y, go.transform.Rotation, point.x, point.y);
    //    }
    //}
    //public class CircleCollider : Collider
    //{
    //    public float Radius;
    //    public CircleCollider(GameObject gameObject, float radius, bool isTrigger = false)
    //    {
    //        Radius = radius;
    //        IsTrigger = isTrigger;
    //        go = gameObject;
    //    }
    //    public override bool Intersect(vec2 point)
    //    {
    //        return unmanaged_Canvas.TestCirc(go.transform.Position.x, go.transform.Position.y, Radius, point.x, point.y);
    //    }
    //}
}
