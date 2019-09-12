using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using UnityEngine;

namespace Traitor_God
{
    internal class Traitor : MonoBehaviour
    {
        /* Health values */
        private const int Phase1Health = 500;
        private const int Phase2Health = 1000;
        private const int Phase3Health = 1000;
        private const int TotalHealth = Phase1Health + Phase2Health + Phase3Health;

        /* DSlash targeting values */
        private int _dSlashSpeed = 75;
        private Vector2 _heroPos;
        private Vector2 _traitorPos;

        /* Spear throw values */
        private int markerSpeed = 250;
        private int _spearThrowSpeed = 75;

        /* Phase Boolean triggers */
        private bool _enteredPhase2;
        private bool _enteredPhase3;

        /* Color of infection trails */
        private static Color infectionOrange = new Color32(255, 50, 0, 255);

        /* Components */
        private tk2dSpriteAnimator _anim;
        private AudioSource _audio;
        public static PlayMakerFSM Control;     // Control is accessed by TinkSound, so make it public and static
        private PlayMakerFSM _gpzControl;
        private HealthManager _hm;
        private Rigidbody2D _rb;

        /* Audio clips */
        private AudioClip _dSlashAudio;
        private AudioClip _jumpAnticAudio;
        private AudioClip _jumpAudio;
        private AudioClip _landAudio;
        private AudioClip _roarAudio;
        private AudioClip _slamAudio;
        private AudioClip _slashAnticAudio;
        private AudioClip _slashAudio;

        /* Trail left behind by Traitor God */
        private ParticleSystem _trail;

        private void Awake()
        {
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _audio = gameObject.GetComponent<AudioSource>();
            Control = gameObject.LocateMyFSM("Mantis");
            /* Used to spawn shockwaves */
            _gpzControl = TraitorGod.PreloadedGameObjects["GPZ"].LocateMyFSM("Control");
            _hm = gameObject.GetComponent<HealthManager>();
            _rb = gameObject.GetComponent<Rigidbody2D>();

            Log("Using alt version: " + PlayerData.instance.statueStateTraitorLord.usingAltVersion);
            Log("Using alt version2: " + PlayerData.instance.GetVariable<BossStatue.Completion>("statueStateTraitor").usingAltVersion);
            if (PlayerData.instance.statueStateTraitorLord.usingAltVersion)
            {
                _hm.hp = TotalHealth;
# if DEBUG
                /* Enter phase 3 immediately */
                _hm.hp = 999;
# endif

                _hm.OnDeath += DeathHandler;
            }
        }

        private IEnumerator Start()
        {
            yield return null;

            while (HeroController.instance == null)
            {
                yield return null;
            }

            if (!PlayerData.instance.statueStateTraitorLord.usingAltVersion)
            {
                /* Revert to old Traitor Lord and slam wave textures if fighting regular Traitor Lord */
                ResetTextures();

                yield break;
            }

            _trail = AddTrail(gameObject, 4, 0.8f, 1.5f, 2, 1.8f, infectionOrange);

            /* Disable empty walks */
            Control.RemoveTransition("Feint?", "Feint");

            /* Disable slam repeat */
            Control.RemoveTransition("Check L", "Repeat?");
            Control.RemoveTransition("Check R", "Repeat?");
            Control.RemoveTransition("Repeat?", "Repeat");
            Control.RemoveTransition("Repeat", "Too Close?");

            /* Add trail to sickles on Sickle Throw */
            Control.InsertMethod("Sickle Throw Recover", 0, AddSickleTrails);
            /* Target the player during DSlash */
            Control.InsertMethod("DSlash Antic", 0, DSlashTargetPlayer);
            /* Summon two sickles when slashing */
            Control.InsertMethod("Attack Swipe", 0, SpawnSickles(40, 2));
            /* Spawn shockwaves when landing after a DSlash */
            Control.InsertMethod("Land", 0, SpawnShockwaves(1, 1f, 25, 2));

            /* Loop DSlash animation */
            _anim.GetClipByName("DSlash").wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;

            Log("Changing to New Sprites");
            /* Change sprites using Mola's Traitor God sprite sheets */
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture =
                TraitorGod.Sprites[0].texture;
            /* Change color of waves using Mola's waves sprite sheet */
            ChangeWaveSprite(1);

            /* Add new moves to Phase 1 */
            AddGroundPound();
            AddWallFall();

            /* Miscellaneous */
            ChangeStateValues();
            GetAudioClips();
        }

        /* Transition to Wall Fall state when Traitor God is in the DSlash state,
           encounters a wall or roof, and is off the ground */
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Control.ActiveStateName == "DSlash" && collision.collider.gameObject.layer == 8 && transform.position.y > 31.6)
            {
                Control.SetState("Wall Fall");
            }
        }

        private void Update()
        {
            if (PlayerData.instance.statueStateTraitorLord.usingAltVersion)
            {
                /* Hacky method of dividing fight into 3 phases */
                if (_hm.hp < (Phase2Health + Phase3Health) && !_enteredPhase2)
                {
                    Log("Entered Phase 2");
                    _enteredPhase2 = true;
                    AddDoubleSlam();
                }
                else if (_hm.hp < Phase3Health && !_enteredPhase3)
                {
                    Log("Entered Phase 3");
                    _enteredPhase3 = true;
                    AddSpearThrow();
                    AddThornPillars();
                }
            }
        }

        private static void Log(object message) => TraitorFinder.Log(message);

        private void SetThornPillarVelocity(float y)
        {
            foreach (GameObject thorns in thornsList)
            {
                thorns.GetComponent<Rigidbody2D>().velocity = new Vector2(0, y);
            }
        }

        /* Retract all existing thorn pillars and destroy them */
        private IEnumerator RectractThornPillarsAndDestroy()
        {
            SetThornPillarVelocity(120);
            yield return new WaitForSeconds(1.0f);
            ClearGameObjectList(thornsList);
        }

        /* Begin Coroutine to retract and destroy any existing thorn pillars upon boss death */
        private void DeathHandler()
        {
            StartCoroutine(RectractThornPillarsAndDestroy());
        }

        private void ResetTextures()
        {
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = TraitorGod.Sprites[3].texture;
            ChangeWaveSprite(4);
        }

        /* Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/Prince.cs */
        private static ParticleSystem AddTrail(GameObject go, int startSize, float lifetime, float shapeRadius, int damage, float offset = 0, Color? c = null)
        {
            ParticleSystem trail = go.AddComponent<ParticleSystem>();
            if (trail == null)
            {
                //  NullReferenceException is returned after the third sickle throw when AddTrail is applied to the sickles because trail is null
                return new ParticleSystem();
            }
            ParticleSystemRenderer rend = trail.GetComponent<ParticleSystemRenderer>();
            rend.material = rend.trailMaterial = new Material(Shader.Find("Particles/Additive (Soft)"))
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

        /* Add a trail to all GameObjects with the name "Shot Traitor Lord(Clone)" */
        private void AddSickleTrails()
        {
            IEnumerable<GameObject> sickles = FindObjectsOfType<GameObject>().Where(obj => obj.name == "Shot Traitor Lord(Clone)");
            foreach (GameObject sickle in sickles)
            {
                sickle.GetComponent<DamageHero>().damageDealt = 2;
                AddTrail(sickle, 2, 0.5f, 0.75f, 2, 0, infectionOrange);
            }
        }

        /* Return vector from current position of Traitor God to the current position of the Knight */
        private Vector2 GetVectorToPlayer()
        {
            _heroPos = HeroController.instance.transform.position;
            _traitorPos = transform.position;

            Vector2 distanceVector = new Vector2(_heroPos.x - _traitorPos.x, _heroPos.y - _traitorPos.y);
            distanceVector.Normalize();
            /*_distanceVector.x += Math.Sign(_distanceVector.x) * 
                                 (_distanceVector.x > 0 ? Math.Sign(HeroController.instance.GetComponent<Rigidbody2D>().velocity.x) : 
                                     -Math.Sign(HeroController.instance.GetComponent<Rigidbody2D>().velocity.x)) * 0.35f;*/
            return distanceVector;
        }

        /* Always target the player on DSlash */
        private void DSlashTargetPlayer()
        {
            Vector2 dSlashVector = GetVectorToPlayer() * _dSlashSpeed; ;

            Control.GetAction<SetVelocity2d>("DSlash").x = dSlashVector.x;
            Control.GetAction<SetVelocity2d>("DSlash").y = dSlashVector.y;
        }

        /* Spawn shockwaves on either side */
        private Action SpawnShockwaves(float width, float height, float speed, int damage)
        {
            return () =>
            {
                Quaternion angle = Quaternion.Euler(Vector3.zero);
                Transform trans = transform;
                Vector3 pos = trans.position;

                bool[] facingRightBools = { false, true };

                foreach (bool @bool in facingRightBools)
                {
                    GameObject shockwave = Instantiate(_gpzControl.GetAction<SpawnObjectFromGlobalPool>("Land Waves")
                        .gameObject.Value);
                    PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                    shockFSM.transform.localScale = new Vector2(height, width);
                    shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = @bool;
                    shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                    shockwave.AddComponent<DamageHero>().damageDealt = damage;
# if DEBUG
                    shockwave.AddComponent<DebugColliders>();
# endif
                    GameObject plane = shockwave.FindGameObjectInChildren("Plane");

                    plane.GetComponent<MeshRenderer>().material.mainTexture = TraitorGod.Sprites[1].texture;
                    shockwave.SetActive(true);
                    shockwave.transform.SetPosition2D(new Vector2(pos.x, 28.1f));
                }
            };
        }

        /* Spawn sickles during slash */
        private Action SpawnSickles(float speed, int damage)
        {
            return () =>
            {
                Quaternion angle = Quaternion.Euler(Vector3.zero);
                Transform trans = transform;
                Vector3 pos = trans.position;
                float x = speed * Math.Sign(trans.localScale.x);

                float[] sicklePositionYs = { pos.y - 2, pos.y };

                foreach (float sicklePosY in sicklePositionYs)
                {
                    GameObject sickle = Instantiate(Control
                        .GetAction<SpawnObjectFromGlobalPool>("Sickle Throw")
                        .gameObject.Value, pos.SetY(sicklePosY), angle);

                    sickle.GetComponent<Rigidbody2D>().velocity = new Vector2(x, 0);
                    sickle.GetComponent<DamageHero>().damageDealt = damage;
                    sickle.name = "Slash Sickle";   // Differentiate these sickles from the ones thrown in a sinusoid
                    Destroy(sickle.GetComponent<AudioSource>());
                    Destroy(sickle.GetComponent<PlayMakerFSM>());
                    Destroy(sickle, 2.0f);
                }
            };
        }

        /* Vertical ground pound, indicated by a red trail */
        private void AddGroundPound()
        {
            string[] states =
            {
                "Ground Pound Jump Antic",
                "Ground Pound Jump",
                "Ground Pound Fall",
                "Ground Pound Land",
                "Ground Pound Recover",
            };

            Control.CreateStates(states);

            _audio.time = 0.15f;

            /* Telegraph ground pound with lower pitched DSlash growl */
            IEnumerator GroundPoundJumpAntic()
            {
                ParticleSystem.MainModule main = _trail.main;
                main.startColor = Color.red;

                _anim.Play("Jump Antic");
                _audio.pitch = 0.9f;
                _audio.PlayOneShot(_jumpAnticAudio);
                _rb.velocity = new Vector2(0, 0);

                yield return new WaitForSeconds(0.25f);
                yield return new WaitForSeconds(0.25f);
            }
            Control.InsertCoroutine("Ground Pound Jump Antic", 0, GroundPoundJumpAntic);

            /* Set jump velocity */
            IEnumerator GroundPoundJump()
            {
                _anim.Play("Jump");
                _audio.PlayOneShot(_jumpAudio);
                _rb.velocity = new Vector2(0, 50);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Ground Pound Jump", 0, GroundPoundJump);

            /* Set ground pound fall velocity */
            IEnumerator GroundPoundFall()
            {
                _anim.Play("DSlash");
                _audio.PlayOneShot(_dSlashAudio);
                transform.rotation = Quaternion.Euler(0, 0, -Math.Sign(transform.localScale.x));
                _rb.velocity = new Vector2(0, -60);
                while (transform.position.y > 32)
                {
                    yield return null;
                }
            }
            Control.InsertCoroutine("Ground Pound Fall", 0, GroundPoundFall);

            /* Land and generate taller shockwaves */
            IEnumerator GroundPoundLand()
            {
                _anim.Play("Land");
                GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
                _rb.velocity = new Vector2(0, 0);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                _audio.PlayOneShot(_landAudio);

                yield return null;
            }
            Control.InsertCoroutine("Ground Pound Land", 0, GroundPoundLand);
            Control.InsertMethod("Ground Pound Land", 0, SpawnShockwaves(2, 2, 75, 2));

            /* Revert back to orange trail */
            IEnumerator GroundPoundRecover()
            {
                ParticleSystem.MainModule main = _trail.main;
                main.startColor = infectionOrange;

                yield return new WaitForSeconds(1.0f);
            }
            Control.InsertCoroutine("Ground Pound Recover", 0, GroundPoundRecover);


            Control.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Ground Pound Jump Antic", 0.33f, 1);
        }

        /* Fall down when encountering wall or ceiling */
        private void AddWallFall()
        {
            Control.CreateState("Wall Fall");

            Control.AddTransition("DSlash", "HITWALL", "Wall Fall");
            Control.AddTransition("Wall Fall", FsmEvent.Finished, "Land");

            IEnumerator WallFall()
            {
                _anim.Play("Enter");
                _rb.velocity = new Vector2(0, -40);

                float y = 31.6f;

                while (transform.position.y > y)
                {
                    yield return null;
                }
                Control.SetState("Land");

                yield return null;
            }

            Control.InsertCoroutine("Wall Fall", 0, WallFall);
        }

        /* Change the values of various FSM Actions */
        private void ChangeStateValues()
        {
            /* Increase range of Slash attack */
            Control.Fsm.GetFsmFloat("Attack Speed").Value = 50.0f;

            /* Increase vertical velocity of DSlash jump */
            Control.GetAction<SetVelocity2d>("Jump").y = 40;

            /* Remove cooldown between attacks */
            Control.RemoveAction<Wait>("Cooldown");
            Control.RemoveAction<Wait>("Sick Throw CD");

            /* Decrease duration of slam attack */
            Control.GetAction<Wait>("Waves").time.Value = 0.0f;

            /* Evenly distribute Slash and DSlash attacks and raise max attack repeats */
            Control.GetAction<SendRandomEventV2>("Attack Choice").weights[0].Value = 0.33f;    // Slash weight
            Control.GetAction<SendRandomEventV2>("Attack Choice").weights[1].Value = 0.33f;    // DSlash weight
            Control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[0].Value = 4;      // Slash max repeats
            Control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[1].Value = 4;      // DSlash max repeats

            /* Double the speed of the waves created by slam attack */
            Control.GetAction<SetVelocity2d>("Waves", 2).x = 24; // Right Wave
            Control.GetAction<SetVelocity2d>("Waves", 7).x = -24; // Left Wave

            /* Traitor God can perform slam attack at 2/3 health */
            Control.GetAction<IntCompare>("Slam?").integer2 = Phase2Health + Phase3Health;

            /* Back up for slam at a further distance to compensate for faster waves */
            Control.GetAction<FloatCompare>("Too Close?").float2 = 15.0f;

            /* Set contact damage to 2 */
            Control.GetAction<SetDamageHeroAmount>("Land").damageDealt = 2;
            Control.GetAction<SetDamageHeroAmount>("Attack Recover").damageDealt = 2;

            /* Fall into the arena faster */
            Control.GetAction<SetVelocity2d>("Fall").y = -60;
        }

        /* Assign values of audio clips used in custom attacks */
        private void GetAudioClips()
        {
            _dSlashAudio = Control.GetAction<AudioPlayerOneShot>("DSlash").audioClips[0];
            _jumpAnticAudio = Control.GetAction<AudioPlayerOneShot>("Jump Antic").audioClips[0];
            _jumpAudio = Control.GetAction<AudioPlayerOneShot>("Jump").audioClips[0];
            _landAudio = Control.GetAction<AudioPlayerOneShot>("Land").audioClips[0];
            _roarAudio = Control.GetAction<AudioPlayerOneShot>("Roar").audioClips[0];
            _slamAudio = (AudioClip)Control.GetAction<AudioPlayerOneShotSingle>("Slamming").audioClip.Value;
            _slashAnticAudio = Control.GetAction<AudioPlayerOneShot>("Attack Antic").audioClips[0];
            _slashAudio = Control.GetAction<AudioPlayerOneShot>("Attack 1").audioClips[0];
        }

        /* Two successive slam waves */
        private void AddDoubleSlam()
        {
            string[] states =
            {
                "Double Slam Antic",
                "Double Slam Slamming",
                "Double Slam Waves 1",
                "Double Slam Waves 2",
            };

            Control.CreateStates(states);

            /* Helper function for spawning slam waves */
            void SpawnWaves(float speed, float timeToLive)
            {
                float[] velocities = { -speed, speed };
                Vector2 pos = transform.position;
                Vector3 spawnPos = new Vector3(pos.x, pos.y - 5, 6.4f);
                Quaternion rot = Quaternion.Euler(0, 0, 0); ;

                foreach (float velocity in velocities)
                {
                    GameObject wave = Instantiate(Control.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value, spawnPos, rot);
                    wave.GetComponent<Rigidbody2D>().velocity = new Vector2(velocity, 0);
                    wave.GetComponentInChildren<SpriteRenderer>().flipX = (velocity < 0);
                    Destroy(wave, timeToLive);
                }
            }

            /* Telegraph for slam */
            IEnumerator DoubleSlamAntic()
            {
                _anim.Play("Shockwave Antic");
                _audio.PlayOneShot(_roarAudio);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Antic", 0, DoubleSlamAntic);

            /* During slam */
            IEnumerator DoubleSlamSlamming()
            {
                _anim.Play("Shockwave Attack");
                _audio.PlayOneShot(_slamAudio);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Slamming", 0, DoubleSlamSlamming);

            /* First pair of waves */
            IEnumerator DoubleSlamWaves1()
            {
                _audio.PlayOneShot(_slamAudio);
                SpawnWaves(12, 3);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Waves 1", 0, DoubleSlamWaves1);

            /* Second pair of waves */
            IEnumerator DoubleSlamWaves2()
            {
                SpawnWaves(24, 3);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Waves 2", 0, DoubleSlamWaves2);
            
            Control.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("Double Slam Antic", 0.2f, 2);
        }

        /* Throw vine-wrapped mantis spear */
        float[] _angles;
        private void AddSpearThrow()
        {
            string[] states =
            {
                "Spear Throw Antic",
                "Spear Throw",
                "Spear Throw Recover",
            };

            Control.CreateStates(states);

            Vector2 vectorToTarget = new Vector2();

            IEnumerator SpearThrowAntic()
            {
                _anim.Play("Sickle Throw Antic");
                _audio.pitch = 0.9f;
                _audio.PlayOneShot(_slashAnticAudio);
                _rb.velocity = new Vector2();

                vectorToTarget = GetVectorToPlayer();

                // + 90 degrees because spear sprite is facing down and must be rotated to align with 0 degrees Cartesian
                float angle = (float)Math.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg + 90;
                _angles = new float[] { 
                        angle - 30,
                        angle,
                        angle + 30 
                };

                Vector2 pos = transform.position;
                foreach (float _angle in _angles)
                {
                    /* Marker attributes */
                    Quaternion rot = Quaternion.Euler(0, 0, 0);
                    GameObject marker = Instantiate(TraitorGod.PreloadedGameObjects["Spear"], pos, rot);
                    marker.GetComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[5];
                    marker.SetActive(true);
                    marker.AddComponent<Rigidbody2D>().isKinematic = true;
                    marker.AddComponent<NonBouncer>();
                    float x = (float)Math.Cos((_angle - 90) / Mathf.Rad2Deg);
                    float y = (float)Math.Sin((_angle - 90) / Mathf.Rad2Deg);
                    Vector2 markerVelocity = new Vector2(x, y) * markerSpeed;
                    marker.GetComponent<Rigidbody2D>().velocity = markerVelocity;
                    AddTrail(marker, 1, 1, 0.1f, 0, 0, infectionOrange);
                    Destroy(marker, 5);
                }

                yield return new WaitForSeconds(1.0f);
            }
            Control.InsertCoroutine("Spear Throw Antic", 0, SpearThrowAntic);

            IEnumerator SpearThrow()
            {
                _anim.Play("Sickle Throw Attack");

                Vector2 pos = transform.position;
                foreach (float angle in _angles)
                {
                    /* Spear attributes */
                    Quaternion rot = Quaternion.Euler(0, 0, angle);
                    GameObject spear = Instantiate(TraitorGod.PreloadedGameObjects["Spear"], pos, rot);
                    spear.GetComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[2];
                    spear.SetActive(true);
                    spear.layer = 11;
                    spear.AddComponent<BoxCollider2D>();
# if DEBUG
                    spear.AddComponent<DebugColliders>();
# endif
                    spear.AddComponent<TinkEffect>();
                    spear.AddComponent<TinkSound>();
                    spear.AddComponent<Rigidbody2D>().isKinematic = true;
                    BoxCollider2D spearCollider = spear.GetComponent<BoxCollider2D>();
                    spearCollider.size = new Vector2(1, 12);
                    spear.AddComponent<DamageHero>().damageDealt = 2;
                    spear.AddComponent<NonBouncer>();
                    float x = (float)Math.Cos((angle - 90) / Mathf.Rad2Deg);
                    float y = (float)Math.Sin((angle - 90) / Mathf.Rad2Deg);
                    Vector2 spearVelocity = new Vector2(x, y) * _spearThrowSpeed;
                    spear.GetComponent<Rigidbody2D>().velocity = spearVelocity;
                    AddTrail(spear, 2, 0.25f, 0.5f, 2, 0, infectionOrange);
                    Destroy(spear, 5);
                }

                yield return new WaitForSeconds(0.25f);
            }
            Control.InsertCoroutine("Spear Throw", 0, SpearThrow);

            IEnumerator SpearThrowRecover()
            {
                _anim.Play("Attack Recover");

                yield return new WaitForSeconds(1.0f);
            }
            Control.InsertCoroutine("Spear Throw Recover", 0, SpearThrowRecover);

            Control.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Spear Throw Antic", 0.33f, 1);
        }

        /* AOE thorn pillars */
        private List<GameObject> thornsList = new List<GameObject>();   // List containing all thorn GameObjects
        private void AddThornPillars()
        {
            string[] states =
            {
                "Thorn Pillars Appear",
                "Thorn Pillars Appear Pause",
                "Thorn Pillars Drop",
                "Thorn Pillars Drop Pause",
                "Thorn Pillars Retract",
                "Thorn Pillars Recover",
            };
            
            Control.CreateStates(states);

            /* Spawn thorn pillars and move them slightly into view at the top of the arena */
            IEnumerator ThornPillarsAppear()
            {
                _audio.time = 0.25f;
                _audio.pitch = 0.9f;
                _audio.PlayOneShot(_roarAudio);
                _anim.Play("Roar");
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

                void SpawnThorns(string preloadedObjectName, Vector2 colliderSize, Vector3 position)
                {
                    GameObject thorns = Instantiate(TraitorGod.PreloadedGameObjects[preloadedObjectName]);
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
            
                /* Spawn thorn pillars on left side of Traitor God */
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
                
                /* Spawn thorn pillars on right side of Traitor God */
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
            Control.InsertCoroutine("Thorn Pillars Appear", 0, ThornPillarsAppear);

            /* Pause briefly to allow player to react */
            IEnumerator ThornPillarsAppearPause()
            {
                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Thorn Pillars Appear Pause", 0, ThornPillarsAppearPause);
            
            /* Drop thorn pillars */
            IEnumerator ThornPillarsDrop()
            {
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                SetThornPillarVelocity(-80);

                yield return new WaitForSeconds(0.25f);
            }
            Control.InsertCoroutine("Thorn Pillars Drop", 0, ThornPillarsDrop);
            
            /* Keep pillars fully dropped for half a second */
            IEnumerator ThornPillarsDropPause()
            {
                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                _audio.PlayOneShot(_landAudio);

                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);


            }
            Control.InsertCoroutine("Thorn Pillars Drop Pause", 0, ThornPillarsDropPause);

            /* Retract pillars */
            IEnumerator ThornPillarsRetract()
            {
                SetThornPillarVelocity(90);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Thorn Pillars Retract", 0, ThornPillarsRetract);

            /* Remove thorn pillars */
            IEnumerator ThornPillarsRecover()
            {
                ClearGameObjectList(thornsList);
                _anim.Play("Idle");
                
                yield return null;
            }
            Control.InsertCoroutine("Thorn Pillars Recover", 0, ThornPillarsRecover);
            
            Control.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("Thorn Pillars Appear", 0.15f, 1);
        }
        
        private void ClearGameObjectList(List<GameObject> gameObjectList)
        {
            foreach (GameObject go in gameObjectList)
            {
                Destroy(go);
            }
            gameObjectList.Clear();
        }

        /* Change slam wave sprite sheet using index of TraitorGod.SPRITES variable */
        private void ChangeWaveSprite(int spriteIndex)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(position);
            GameObject wave = Instantiate(Control.GetAction<SpawnObjectFromGlobalPool>("Waves").gameObject.Value,
                position, rotation);
            byte[] spriteSheetByteData = TraitorGod.SpriteBytes[spriteIndex];
            wave.GetComponentInChildren<SpriteRenderer>().sprite.texture.LoadImage(spriteSheetByteData);
            Destroy(wave);
        }
    }
}