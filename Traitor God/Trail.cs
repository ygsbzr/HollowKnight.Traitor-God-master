using System.Linq;
using UnityEngine;

namespace Traitor_God
{
    public class Trail
    {
        /* Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/Prince.cs */
        public static ParticleSystem AddTrail(GameObject go, 
                                              float startSize, 
                                              float lifetime, 
                                              float shapeRadius, 
                                              int damage, 
                                              float offset = 0, 
                                              Color? c = null)
        {
            ParticleSystem trail = go.AddComponent<ParticleSystem>();
            if (trail == null)
            {
                //  NullReferenceException is returned after the third sickle throw when AddTrail is applied to the sickles because trail is null
                return new ParticleSystem();
            }
            ParticleSystemRenderer rend = trail.GetComponent<ParticleSystemRenderer>();
            rend.material = rend.trailMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive (Soft)"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = c ?? Color.white
            };

            ParticleSystem.MainModule main = trail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startSize = startSize;
            main.startLifetime = lifetime;
            main.startColor = c ?? Color.white;
            main.maxParticles = 256;

            ParticleSystem.ShapeModule shape = trail.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius *= shapeRadius;
            shape.radiusSpeed = 0.01f;
            Vector3 pos = shape.position;
            pos.y -= offset;
            shape.position = pos;

            ParticleSystem.EmissionModule emission = trail.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 10f;

            ParticleSystem.CollisionModule collision = trail.collision;
            collision.type = ParticleSystemCollisionType.World;
            collision.sendCollisionMessages = true;
            collision.mode = ParticleSystemCollisionMode.Collision2D;
            collision.enabled = true;
            collision.quality = ParticleSystemCollisionQuality.High;
            collision.maxCollisionShapes = 256;
            collision.dampenMultiplier = 0;
            collision.radiusScale = .3f;
            collision.collidesWith = 1 << 9;

            go.AddComponent<DamageOnCollision>().Damage = damage;

            return trail;
        }

        private static void Log(object message) => Modding.Logger.Log($"[Trail]: " + message);
    }
}
