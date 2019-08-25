using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using Modding;
using On.InControl;
using UnityEngine;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Traitor_God
{
    internal class Traitor : MonoBehaviour
    {
        private const int HP = 2000;
        private HealthManager _hm;

        private Vector2 _heroPos;
        private Vector2 _traitorPos;
        private Vector2 _distanceVector;
        private Vector2 _dSlashVector;
        private int _dSlashSpeed = 75;

        private tk2dSpriteAnimator _anim;

        private PlayMakerFSM _control;

        private readonly Random _rand = new Random();

        private ParticleSystem _trail;

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _control = gameObject.LocateMyFSM("Mantis");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
        }

        private GameObject _silentSickle;
        
        private GameObject SilentSickle
        {
            get
            {
                if (_silentSickle != null) return _silentSickle;

                _silentSickle =
                    Instantiate(_control.GetAction<SpawnObjectFromGlobalPool>("Sickle Throw")
                        .gameObject.Value);
                
                Destroy(_silentSickle.GetComponent<AudioSource>());
                _silentSickle.tag = "silentSickle";

                return _silentSickle;
            }
        }

        private void ChangeStateValues()
        {
            // Increase range of Slash attack
            _control.Fsm.GetFsmFloat("Attack Speed").Value = 70.0f;
            
            // Increase vertical velocity of DSlash jump
            _control.GetAction<SetVelocity2d>("Jump").y = 40;

            // Remove the cooldown between attacks
            _control.RemoveAction<Wait>("Cooldown");
            _control.RemoveAction<Wait>("Sick Throw CD");

            // Decrease duration of slam attack
            _control.GetAction<Wait>("Waves").time.Value = 0.0f;

            // More evenly distribute Slash and DSlash attacks
            _control.GetAction<SendRandomEventV2>("Attack Choice").weights[0].Value = 0.5f;    // Slash weight
            _control.GetAction<SendRandomEventV2>("Attack Choice").weights[1].Value = 0.5f;    // DSlash weight
            _control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[0].Value = 4;      // Slash max repeats
            _control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[1].Value = 4;      // DSlash max repeats

            // Double the speed of the waves created by slam attack
            _control.GetAction<SetVelocity2d>("Waves", 2).x = 24; // Right Wave
            _control.GetAction<SetVelocity2d>("Waves", 7).x = -24; // Left Wave

            // Traitor Lord can perform slam attack at 2/3 health
            _control.GetAction<IntCompare>("Slam?").integer2 = HP * 2 / 3;

            // Make Traitor Lord back up for the slam at a further distance to compensate for faster waves
            _control.GetAction<FloatCompare>("Too Close?").float2 = 15.0f;

            // Set contact damage to 2
            _control.GetAction<SetDamageHeroAmount>("Land").damageDealt = 2;
            _control.GetAction<SetDamageHeroAmount>("Attack Recover").damageDealt = 2;
            
        }

        /* Always target the player on DSlash */
        private void DSlashTargetPlayer()
        {
            _heroPos = HeroController.instance.transform.position;
            _traitorPos = transform.position;

            _distanceVector = new Vector2(_heroPos.x - _traitorPos.x, _heroPos.y - _traitorPos.y);
            _distanceVector.Normalize();
            // Predictive player tracking
            _distanceVector.x += Math.Sign(_distanceVector.x) * 
                                 (_distanceVector.x > 0 ? Math.Sign(HeroController.instance.GetComponent<Rigidbody2D>().velocity.x) : 
                                     -Math.Sign(HeroController.instance.GetComponent<Rigidbody2D>().velocity.x)) * 0.35f;
            _dSlashVector = _distanceVector * _dSlashSpeed;

            _control.GetAction<SetVelocity2d>("DSlash").x = _dSlashVector.x;
            _control.GetAction<SetVelocity2d>("DSlash").y = _dSlashVector.y;
        }

        private Action SpawnShockwaves(float height, float speed, int damage)
        {
            return () =>
            {
                Quaternion angle = Quaternion.Euler(Vector3.zero);
                Transform trans = transform;
                Vector3 pos = trans.position;

                bool[] facingRightBools = {false, true};

                foreach (bool @bool in facingRightBools)
                {
                    PlayMakerFSM gpzControl = TraitorGod.preloadedGameObjects["GPZ"].LocateMyFSM("Control");
                    GameObject shockWave = Instantiate(gpzControl.GetAction<SpawnObjectFromGlobalPool>("Land Waves")
                        .gameObject.Value);
                    PlayMakerFSM shock = shockWave.LocateMyFSM("shockwave");
                    shock.transform.localScale = new Vector2(height, 1.0f);
                    shock.FsmVariables.FindFsmBool("Facing Right").Value = @bool;
                    shock.FsmVariables.FindFsmFloat("Speed").Value = speed;
                    shockWave.AddComponent<DamageHero>().damageDealt = damage;
                    shockWave.SetActive(true);
                    shockWave.transform.SetPosition2D(new Vector2(pos.x, 28.1f));    
                }
            };
        }

        private Action SetAnimFPS(int fps)
        {
            return () => { _anim.CurrentClip.fps = fps; };
        }

        private GameObject _sickle1;
        private GameObject _sickle2;

        private IEnumerator Start()
        {
            yield return null;

            while (HeroController.instance == null)
            {
                yield return null;
            }
            
            _trail = AddTrail(gameObject, 1.8f);

            // Disable empty walks
            _control.RemoveTransition("Feint?", "Feint");

            // Disable slam repeat
            _control.RemoveTransition("Check L", "Repeat?");
            _control.RemoveTransition("Check R", "Repeat?");
            _control.RemoveTransition("Repeat?", "Repeat");
            _control.RemoveTransition("Repeat", "Too Close?");

            ChangeStateValues();

            Action ProjectileSpawner(Func<GameObject> projectile, float speed)
            {
                return () =>
                {
                    Quaternion angle = Quaternion.Euler(Vector3.zero);
                    Transform trans = transform;
                    Vector3 pos = trans.position;
                    float x = speed * Math.Sign(trans.localScale.x);

                    float[] sicklePositionYs = {pos.y - 2, pos.y};

                    foreach (float sicklePosY in sicklePositionYs)
                    {
                        GameObject sickle = Instantiate(_control
                            .GetAction<SpawnObjectFromGlobalPool>("Sickle Throw")
                            .gameObject.Value, pos.SetY(sicklePosY), angle);
                        sickle.GetComponent<Rigidbody2D>().velocity = new Vector2(x, -15);
                        Destroy(sickle.GetComponent<AudioSource>());
                    }
                };
            }

            // Speed up and decrease time of Slash telegraph
            _control.InsertMethod("Attack 1", 0, SetAnimFPS(18));
            
            // Summon two sickles when slashing
            _control.InsertMethod("Attack Swipe", 0, ProjectileSpawner( () => SilentSickle, 40f));
            
            // Target the player during DSlash
            _control.InsertMethod("DSlash Antic", 0, DSlashTargetPlayer);
            
            //_control.InsertMethod("DSlash", 0, DetectWalls);
            _control.InsertMethod("Land", 0, SpawnShockwaves(0.5f, 25f, 2));

            _hm.hp = HP;

            AddWallFall();

            _control.GetAction<SetVelocity2d>("Fall").y = -90;

        }

        private Vector2 _sick1Pos;
        private Vector2 _sick2Pos;
        private Vector2 _sickVel;
        /*private void Update()
        {

            _sick1Pos = _sickle1.transform.position;
            _sick2Pos = _sickle2.transform.position;

            _sick1Pos.x += 50;
            _sick2Pos.x -= 50;
        }*/

        private void DetectWalls()
        {
            if (transform.position.x > 58 || transform.position.x < 23 || transform.position.y > 42)
            {
                _control.SetState("Wall Fall");
            }
        }

        private void AddWallFall()
        {
            _control.CreateState("Wall Fall");
            //_control.ChangeTransition("DSlash", "HITWALL", "Wall Fall");
            _control.AddTransition("DSlash", FsmEvent.CollisionEnter2D, "Wall Fall");
            _control.AddTransition("Wall Fall", FsmEvent.Finished, "Land");

            _control.GetAction<Tk2dPlayAnimation>("Jump Antic").clipName = "Fall";
            
            Action WallFallAntic()
            {
                return () =>
                {
                    _control.GetAction<GetVelocity2d>("DSlash").x = 0;
                    _control.GetAction<GetVelocity2d>("DSlash").y = -1;
                    _anim.Play("Enter");
                };
            }
            
            _control.InsertMethod("Wall Fall", 0, WallFallAntic());
        }

        private static ParticleSystem AddTrail(GameObject go, float offset = 0, Color? c = null)
        {
            var trail = go.AddComponent<ParticleSystem>();
            var rend = trail.GetComponent<ParticleSystemRenderer>();

            Color trailColor = new Color32(255, 50, 0, 255);

            rend.material = rend.trailMaterial = new Material(Shader.Find("Particles/Additive (Soft)"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = c ?? trailColor
            };
            

            ParticleSystem.MainModule main = trail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startSize = 4;
            main.startLifetime = .8f;
            main.startColor = c ?? trailColor;
            main.maxParticles = 256;

            ParticleSystem.ShapeModule shape = trail.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius *= 1.5f;
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
            
            go.AddComponent<DamageOnCollision>().Damage = 2;

            return trail;
        }
    }
}