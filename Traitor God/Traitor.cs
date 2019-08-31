﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using On.TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Traitor_God
{
    internal class Traitor : MonoBehaviour
    {
        private const int HP = 2000;

        private Vector2 _heroPos;
        private Vector2 _traitorPos;
        private Vector2 _distanceVector;
        private Vector2 _dSlashVector;
        private int _dSlashSpeed = 75;

        private static Color infectionOrange = new Color32(255, 50, 0, 255);

        private tk2dSpriteAnimator _anim;
        private AudioSource _audio;
        private PlayMakerFSM _control;
        private HealthManager _hm;
        private Rigidbody2D _rb;

        private readonly Random _rand = new Random();

        private AudioClip _dSlashAudio;
        private AudioClip _jumpAnticAudio;
        private AudioClip _jumpAudio;
        private AudioClip _landAudio;
        private AudioClip _roarAudio;
        private AudioClip _slashAudio;

        private ParticleSystem _trail;

        private void Awake()
        {
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _audio = gameObject.GetComponent<AudioSource>();
            _control = gameObject.LocateMyFSM("Mantis");
            _hm = gameObject.GetComponent<HealthManager>();
            _rb = gameObject.GetComponent<Rigidbody2D>();

            _hm.OnDeath += DestroyThornPillars;
            
            _audio.pitch = 0.9f;
        }

        private void GetAudioClips()
        {
            _dSlashAudio = _control.GetAction<AudioPlayerOneShot>("DSlash").audioClips[0];
            _jumpAnticAudio = _control.GetAction<AudioPlayerOneShot>("Jump Antic").audioClips[0];
            _jumpAudio = _control.GetAction<AudioPlayerOneShot>("Jump").audioClips[0];
            _landAudio = _control.GetAction<AudioPlayerOneShot>("Land").audioClips[0];
            _roarAudio = _control.GetAction<AudioPlayerOneShot>("Roar").audioClips[0];
            _slashAudio = _control.GetAction<AudioPlayerOneShot>("Attack 1").audioClips[0];
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

            // Evenly distribute Slash and DSlash attacks and raise max attack repeats
            _control.GetAction<SendRandomEventV2>("Attack Choice").weights[0].Value = 0.33f;    // Slash weight
            _control.GetAction<SendRandomEventV2>("Attack Choice").weights[1].Value = 0.33f;    // DSlash weight
            _control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[0].Value = 4;      // Slash max repeats
            _control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[1].Value = 4;      // DSlash max repeats

            // Double the speed of the waves created by slam attack
            _control.GetAction<SetVelocity2d>("Waves", 2).x = 24; // Right Wave
            _control.GetAction<SetVelocity2d>("Waves", 7).x = -24; // Left Wave

            // Traitor God can perform slam attack at 2/3 health
            _control.GetAction<IntCompare>("Slam?").integer2 = HP * 2 / 3;

            // Make Traitor God back up for the slam at a further distance to compensate for faster waves
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
            // Predictive player tracking based on player movement
            _distanceVector.x += Math.Sign(_distanceVector.x) * 
                                 (_distanceVector.x > 0 ? Math.Sign(HeroController.instance.GetComponent<Rigidbody2D>().velocity.x) : 
                                     -Math.Sign(HeroController.instance.GetComponent<Rigidbody2D>().velocity.x)) * 0.35f;
            _dSlashVector = _distanceVector * _dSlashSpeed;

            _control.GetAction<SetVelocity2d>("DSlash").x = _dSlashVector.x;
            _control.GetAction<SetVelocity2d>("DSlash").y = _dSlashVector.y;
        }

        private Action SpawnShockwaves(float width, float height, float speed, int damage)
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
                    GameObject shockwave = Instantiate(gpzControl.GetAction<SpawnObjectFromGlobalPool>("Land Waves")
                        .gameObject.Value);
                    PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                    shockFSM.transform.localScale = new Vector2(height, width);
                    shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = @bool;
                    shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                    shockwave.AddComponent<DamageHero>().damageDealt = damage;
                    shockwave.SetActive(true);
                    shockwave.transform.SetPosition2D(new Vector2(pos.x, 28.1f));
                }
            };
        }

        private Action SetAnimFPS(int framesPerSecond)
        {
            return () => { _anim.CurrentClip.fps = framesPerSecond; };
        }

        private void ClearGameObjectList(List<GameObject> gameObjectList)
        {
            foreach (GameObject gameObject in gameObjectList)
            {
                Destroy(gameObject);
            }
            gameObjectList.Clear();
        }

        private IEnumerator RectractThornPillarsAndDestroy()
        {
            SetThornPillarVelocity(120);
            yield return new WaitForSeconds(1.0f);
            ClearGameObjectList(thornsList);
        }
        
        private void DestroyThornPillars()
        {
            StartCoroutine(RectractThornPillarsAndDestroy());
        }
        
        private void AddGroundPound()
        {
            string[] states =
            {
                "Ground Pound Jump Antic",
                "Ground Pound Jump",
                "Ground Pound Fall Antic",
                "Ground Pound Fall",
                "Ground Pound Land",
                "Ground Pound Recover",
            };
            
            for (int i = 0; i < states.Length; i++)
            {
                string state = states[i];
                _control.CreateState(state);
                _control.AddTransition(state, FsmEvent.Finished,
                    i + 1 < states.Length ? states[i + 1] : "Idle");
            }
            
            _audio.time = 0.15f;
            
            IEnumerator GroundPoundJumpAntic()
            {
                ParticleSystemRenderer psr = _trail.GetComponent<ParticleSystemRenderer>();
                psr.material.shader = Shader.Find("Particles/Additive (Soft)");
                ParticleSystem.MainModule main = _trail.main;
                main.startColor = Color.red;
                
                _anim.Play("Jump Antic");
                _audio.PlayOneShot(_jumpAnticAudio);
                _rb.velocity = new Vector2(0, 0);

                yield return new WaitForSeconds(0.25f);
            }
            _control.InsertCoroutine("Ground Pound Jump Antic", 0, GroundPoundJumpAntic);
            
            IEnumerator GroundPoundJump()
            {
                _anim.Play("Jump");
                _audio.PlayOneShot(_jumpAudio);
                _rb.velocity = new Vector2(0, 50);
                
                yield return new WaitForSeconds(1.5f);
            }
            _control.InsertCoroutine("Ground Pound Jump", 0, GroundPoundJump);

            IEnumerator GroundPoundFallAntic()
            {
                yield return null;
            }
            _control.InsertCoroutine("Ground Pound Fall Antic", 0, GroundPoundFallAntic);
            
            IEnumerator GroundPoundFall()
            {
                _anim.Play("DSlash");
                _audio.PlayOneShot(_dSlashAudio);
                transform.rotation = Quaternion.Euler(0, 0, -Math.Sign(transform.localScale.x));
                _rb.velocity = new Vector2(0, -150);
                while (transform.position.y > 32)
                {
                    yield return null;
                }
            }
            _control.InsertCoroutine("Ground Pound Fall", 0, GroundPoundFall);
            
            IEnumerator GroundPoundLand()
            {
                _anim.Play("Land");
                _rb.velocity = new Vector2(0, 0);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                _audio.PlayOneShot(_landAudio);
                
                yield return null;
            }
            _control.InsertCoroutine("Ground Pound Land", 0, GroundPoundLand);
            _control.InsertMethod("Ground Pound Land", 0, SpawnShockwaves(2, 2, 75, 2));

            IEnumerator GroundPoundRecover()
            {
                ParticleSystemRenderer psr = _trail.GetComponent<ParticleSystemRenderer>();
                psr.material.shader = Shader.Find("Particles/Additive (Soft)");
                ParticleSystem.MainModule main = _trail.main;
                main.startColor = infectionOrange;
                
                yield return new WaitForSeconds(1.0f);
                yield return new WaitForSeconds(1.0f);
            }
            _control.InsertCoroutine("Ground Pound Recover", 0, GroundPoundRecover);

            
            _control.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Ground Pound Jump Antic", 0.33f, 1);
        }

        void SetThornPillarVelocity(float y)
        {
            foreach (GameObject thorns in thornsList)
            {
                thorns.GetComponent<Rigidbody2D>().velocity = new Vector2(0, y);
            }
        }
        
        private List<GameObject> thornsList = new List<GameObject>();
        private void AddThornPillars()
        {

            string[] states =
            {
                "ThornPillars Appear",
                "ThornPillars Appear Pause",
                "ThornPillars Drop",
                "ThornPillars Drop Pause",
                "ThornPillars Retract",
                "ThornPillars Recover",
            };
            
            
            for (int i = 0; i < states.Length; i++)
            {
                string state = states[i];
                _control.CreateState(state);
                _control.AddTransition(state, FsmEvent.Finished, 
                    i + 1 < states.Length ? states[i + 1] : "Idle");
            }

            // Spawn thorn pillars and move them slightly into view at the top of the arena
            IEnumerator ThornPillarsAppear()
            {
                _audio.time = 0.25f;
                _audio.PlayOneShot(_roarAudio);
                _anim.Play("Roar");
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

                void SpawnThorns(string preloadedObjectName, Vector2 colliderSize, Vector3 position)
                {
                    GameObject thorns = Instantiate(TraitorGod.preloadedGameObjects[preloadedObjectName]);
                    thorns.SetActive(true);
                    thorns.layer = 17;
                    thorns.AddComponent<BoxCollider2D>();
                    thorns.AddComponent<Rigidbody2D>().isKinematic = true;
                    BoxCollider2D thornsCollider = thorns.GetComponent<BoxCollider2D>();
                    thornsCollider.size = colliderSize;
                    thorns.AddComponent<DamageHero>().damageDealt = 2;    
                    thorns.GetComponent<DamageHero>().hazardType = 0;    // Disable shade cloaking through pillar
                    thorns.AddComponent<NonBouncer>();                   // Disable pogoing on pillar
                    thorns.transform.position = position;
                    thornsList.Add(thorns);
                }
                
                Vector2 pos = transform.position;
            
                // Spawn thorn pillars on left side of Traitor God
                for (float i = pos.x - 5; i >= 0; i -= 6)
                {
                    for (float j = 62f; j <= 105; j += 5)
                    {
                        SpawnThorns("ThornsL", new Vector2(8, 2), new Vector3(i - 1, j, 0));
                        SpawnThorns("BlackThorns", new Vector2(5, 2), new Vector3(i, j, 0));
                        SpawnThorns("ThornsR", new Vector2(8, 2), new Vector3(i + 1, j, 0));
                    }
                    SpawnThorns("ThornPoint", new Vector2(3, 8), new Vector3(i, 59, 0));
                }
                
                // Spawn thorn pillars on right side of Traitor God
                for (float i = pos.x + 5; i <= 75; i += 6)
                {
                    for (float j = 62f; j <= 105; j += 5)
                    {
                        SpawnThorns("ThornsL", new Vector2(8, 2), new Vector3(i - 1, j, 0));
                        SpawnThorns("BlackThorns", new Vector2(5, 2), new Vector3(i, j, 0));
                        SpawnThorns("ThornsR", new Vector2(8, 2), new Vector3(i + 1, j, 0));
                    }
                    SpawnThorns("ThornPoint", new Vector2(3, 8), new Vector3(i, 59, 0));
                }
                
                SetThornPillarVelocity(-60);
                
                yield return new WaitForSeconds(0.25f);
            }
            _control.InsertCoroutine("ThornPillars Appear", 0, ThornPillarsAppear);

            // Pause briefly to allow player to react
            IEnumerator ThornPillarsAppearPause()
            {
                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);
            }
            _control.InsertCoroutine("ThornPillars Appear Pause", 0, ThornPillarsAppearPause);
            
            // Drop thorn pillars
            IEnumerator ThornPillarsDrop()
            {
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                
                
                SetThornPillarVelocity(-80);

                yield return new WaitForSeconds(0.5f);
            }
            _control.InsertCoroutine("ThornPillars Drop", 0, ThornPillarsDrop);
            
            // Keep pillars fully dropped for half a second while gradually fading out roar
            IEnumerator ThornPillarsDropPause()
            {
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);


            }
            _control.InsertCoroutine("ThornPillars Drop Pause", 0, ThornPillarsDropPause);

            // Retract pillars
            IEnumerator ThornPillarsRetract()
            {
                SetThornPillarVelocity(90);

                yield return new WaitForSeconds(0.5f);
            }
            _control.InsertCoroutine("ThornPillars Retract", 0, ThornPillarsRetract);

            // Remove thorn pillars
            IEnumerator ThornPillarsRecover()
            {
                ClearGameObjectList(thornsList);
                _anim.Play("Idle");
                yield return null;
            }
            _control.InsertCoroutine("ThornPillars Recover", 0, ThornPillarsRecover);
            
            _control.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("ThornPillars Appear", 0.15f, 1);
        }
        
        private void AddWallFall()
        {
            _control.CreateState("Wall Fall");

            _control.AddTransition("DSlash", "HITWALL", "Wall Fall");
            _control.AddTransition("Wall Fall", FsmEvent.Finished, "Land");

            IEnumerator WallFall()
            {
                _anim.Play("Enter");
                _rb.velocity = new Vector2(0, -40);

                float y = 31.6f;

                while (transform.position.y > y)
                {
                    yield return null;
                }
                
                _control.SetState("Land");

                yield return null;
            }
            
            _control.InsertCoroutine("Wall Fall", 0, WallFall);
        }
        
        private IEnumerator Start()
        {
            yield return null;

            while (HeroController.instance == null)
            {
                yield return null;
            }
            
            if (PlayerData.instance.statueStateTraitorLord.usingAltVersion == false) yield break;

            _trail = AddTrail(gameObject, 1.8f);

            // Disable empty walks
            _control.RemoveTransition("Feint?", "Feint");

            // Disable slam repeat
            _control.RemoveTransition("Check L", "Repeat?");
            _control.RemoveTransition("Check R", "Repeat?");
            _control.RemoveTransition("Repeat?", "Repeat");
            _control.RemoveTransition("Repeat", "Too Close?");

            GetAudioClips();
            ChangeStateValues();

            Action SpawnSickles(float speed)
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
                        
                        sickle.GetComponent<Rigidbody2D>().velocity = new Vector2(x, 0);
                        sickle.GetComponent<DamageHero>().damageDealt = 2;
                        Destroy(sickle.GetComponent<AudioSource>());
                        Destroy(sickle.GetComponent<PlayMakerFSM>());
                    }
                };
            }

            // Speed up and decrease time of Slash telegraph
            _control.InsertMethod("Attack 1", 0, SetAnimFPS(18));
            // Summon two sickles when slashing
            _control.InsertMethod("Attack Swipe", 0, SpawnSickles(40f));
            // Target the player during DSlash
            _control.InsertMethod("DSlash Antic", 0, DSlashTargetPlayer);
            // Spawn shockwaves when landing after a DSlash
            _control.InsertMethod("Land", 0, SpawnShockwaves(1, 0.5f, 25, 2));

            // Loop animations
            _anim.GetClipByName("DSlash").wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;

            _hm.hp = HP;

            AddGroundPound();
            AddThornPillars();
            AddWallFall();
            
            _control.GetAction<SetVelocity2d>("Fall").y = -90;
        }
        
        private string prevState = "Emerge Dust";
        private Vector2 prevPos = new Vector2();
        private void Update()
        {
            if (transform.position.x != prevPos.x && transform.position.y != prevPos.y)
            {
                //Log("Position: " + transform.position);
                prevPos = transform.position;
            }
                
            if (_control.ActiveStateName != prevState)
            {
                //Log("Current State: " + _control.ActiveStateName);
                prevState = _control.ActiveStateName;
            }
        }

        // Transition to Wall Fall state when Traitor God is in the DSlash state, encounters a wall or roof, and is off the ground
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_control.ActiveStateName == "DSlash" && collision.collider.gameObject.layer == 8 && transform.position.y > 31.6)
            {
                _control.SetState("Wall Fall");
            }
        }
        
        private static ParticleSystem AddTrail(GameObject go, float offset = 0, Color? c = null)
        {
            var trail = go.AddComponent<ParticleSystem>();
            var rend = trail.GetComponent<ParticleSystemRenderer>();

            Color trailColor = infectionOrange;

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

        private static void Log(object message) => TraitorFinder.Log(message);
    }
}