using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fsm = On.HutongGames.PlayMaker.Fsm;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Traitor_God
{
    internal class Traitor : MonoBehaviour
    {
        private const int Phase1Health = 500;
        private const int Phase2Health = 1000;
        private const int Phase3Health = 1000;
        private const int TotalHealth = Phase1Health + Phase2Health + Phase3Health;

        private Vector2 _heroPos;
        private Vector2 _traitorPos;
        private int _dSlashSpeed = 75;

        private bool _enteredPhase2;
        private bool _enteredPhase3;

        private static Color infectionOrange = new Color32(255, 50, 0, 255);

        private tk2dSpriteAnimator _anim;
        private AudioSource _audio;
        // Control is accessed by TinkSound, so make it public and static
        public static PlayMakerFSM Control;
        private PlayMakerFSM _gpzControl;
        private HealthManager _hm;
        private Rigidbody2D _rb;

        private AudioClip _dSlashAudio;
        private AudioClip _jumpAnticAudio;
        private AudioClip _jumpAudio;
        private AudioClip _landAudio;
        private AudioClip _roarAudio;
        private AudioClip _slamAudio;
        private AudioClip _slashAnticAudio;
        private AudioClip _slashAudio;
        
        private ParticleSystem _trail;

        private void AddDoubleSlam()
        {
            string[] states =
            {
                "Double Slam Antic",
                "Double Slam Slamming",
                "Double Slam Waves 1",
                "Double Slam Waves 2",
                "Double Slam Recover",
            };

            Control.CreateStates(states);

            IEnumerator DoubleSlamAntic()
            {
                _anim.Play("Shockwave Antic");
                _audio.PlayOneShot(_roarAudio);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Antic", 0, DoubleSlamAntic);

            IEnumerator DoubleSlamSlamming()
            {
                _anim.Play("Shockwave Attack");
                _audio.PlayOneShot(_slamAudio);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Slamming", 0, DoubleSlamSlamming);

            IEnumerator DoubleSlamWaves1()
            {
                _audio.PlayOneShot(_slamAudio);
                SpawnWaves(12, 3);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Waves 1", 0, DoubleSlamWaves1);

            IEnumerator DoubleSlamWaves2()
            {
                SpawnWaves(24, 3);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Double Slam Waves 2", 0, DoubleSlamWaves2);

            IEnumerator DoubleSlamRecover()
            {
                yield return null;
            }
            Control.InsertCoroutine("Double Slam Recover", 0, DoubleSlamRecover);
            
            Control.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("Double Slam Antic", 0.2f, 2);
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
            
            Control.CreateStates(states);
            
            _audio.time = 0.15f;
            
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
            
            IEnumerator GroundPoundJump()
            {
                _anim.Play("Jump");
                _audio.PlayOneShot(_jumpAudio);
                _rb.velocity = new Vector2(0, 50);
                
                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("Ground Pound Jump", 0, GroundPoundJump);

            IEnumerator GroundPoundFallAntic()
            {
                yield return null;
            }
            Control.InsertCoroutine("Ground Pound Fall Antic", 0, GroundPoundFallAntic);
            
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

            IEnumerator GroundPoundRecover()
            {
                ParticleSystem.MainModule main = _trail.main;
                main.startColor = infectionOrange;

                yield return new WaitForSeconds(1.0f);
                yield return new WaitForSeconds(1.0f);
            }
            Control.InsertCoroutine("Ground Pound Recover", 0, GroundPoundRecover);

            
            Control.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Ground Pound Jump Antic", 0.33f, 1);
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
            
            Control.CreateStates(states);

            // Spawn thorn pillars and move them slightly into view at the top of the arena
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
            Control.InsertCoroutine("ThornPillars Appear", 0, ThornPillarsAppear);

            // Pause briefly to allow player to react
            IEnumerator ThornPillarsAppearPause()
            {
                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("ThornPillars Appear Pause", 0, ThornPillarsAppearPause);
            
            // Drop thorn pillars
            IEnumerator ThornPillarsDrop()
            {
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                SetThornPillarVelocity(-80);

                yield return new WaitForSeconds(0.25f);
            }
            Control.InsertCoroutine("ThornPillars Drop", 0, ThornPillarsDrop);
            
            // Keep pillars fully dropped for half a second while gradually fading out roar
            IEnumerator ThornPillarsDropPause()
            {
                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                _audio.PlayOneShot(_landAudio);

                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);


            }
            Control.InsertCoroutine("ThornPillars Drop Pause", 0, ThornPillarsDropPause);

            // Retract pillars
            IEnumerator ThornPillarsRetract()
            {
                SetThornPillarVelocity(90);

                yield return new WaitForSeconds(0.5f);
            }
            Control.InsertCoroutine("ThornPillars Retract", 0, ThornPillarsRetract);

            // Remove thorn pillars
            IEnumerator ThornPillarsRecover()
            {
                ClearGameObjectList(thornsList);
                _anim.Play("Idle");
                
                yield return null;
            }
            Control.InsertCoroutine("ThornPillars Recover", 0, ThornPillarsRecover);
            
            Control.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("ThornPillars Appear", 0.15f, 1);
        }
        
        float _angle;
        GameObject _thornSpear;
        private void AddThornSpearThrow()
        {
            string[] states =
            {
                "Thorn Spear Throw Antic",
                "Thorn Spear Throw",
                "Thorn Spear Throw Recover",
                "Thorn Spear Throw Destroy Spear"
            };
            
            Control.CreateStates(states);

            Vector2 vectorToTarget = new Vector2(0, 0);

            IEnumerator ThornSpearThrowAntic()
            {
                _anim.Play("Sickle Throw Antic");
                _audio.pitch = 0.9f;
                _audio.PlayOneShot(_slashAnticAudio);
                _rb.velocity = new Vector2(0, 0);
                
                vectorToTarget = GetVectorToPlayer();
                _angle = (float)Math.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg + 90;
                
                yield return new WaitForSeconds(1.0f);
            }
            Control.InsertCoroutine("Thorn Spear Throw Antic", 0, ThornSpearThrowAntic);
            
            IEnumerator ThornSpearThrow()
            {
                _anim.Play("Sickle Throw Attack");

                Vector2 pos = transform.position;
                Log("Angle: " + _angle);
                Quaternion rot = Quaternion.Euler(0, 0, _angle);

                /* Thorn Spear */
                _thornSpear = Instantiate(TraitorGod.PreloadedGameObjects["ThornSpear"], pos, rot);
                _thornSpear.GetComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[2];
                _thornSpear.SetActive(true);
                _thornSpear.layer = 11;
                _thornSpear.AddComponent<BoxCollider2D>();
                _thornSpear.AddComponent<DebugColliders>();
                _thornSpear.AddComponent<TinkEffect>();
                _thornSpear.AddComponent<TinkSound>();
                _thornSpear.AddComponent<Rigidbody2D>().isKinematic = true;
                BoxCollider2D thornSpearCollider = _thornSpear.GetComponent<BoxCollider2D>();
                thornSpearCollider.size = new Vector2(1, 12);
                _thornSpear.AddComponent<DamageHero>().damageDealt = 2;
                _thornSpear.AddComponent<NonBouncer>();
                _thornSpear.GetComponent<Rigidbody2D>().velocity = new Vector2(vectorToTarget.x, vectorToTarget.y) * 75;

                /* Particle Trail */
                ParticleSystem ps = _thornSpear.AddComponent<ParticleSystem>();
                ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();

                psr.material = psr.trailMaterial = new Material(Shader.Find("Particles/Additive (Soft)"))
                {
                    mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                    color = infectionOrange
                };
                
                ParticleSystem.MainModule main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startSpeed = 0f;
                main.startSize = 1;
                main.startLifetime = .8f;
                main.startColor = infectionOrange;
                main.maxParticles = 256;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius *= 1f;
                shape.radiusSpeed = 0.01f;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.rateOverDistance = 10f;

                yield return new WaitForSeconds(0.25f);
            }
            Control.InsertCoroutine("Thorn Spear Throw", 0, ThornSpearThrow);

            IEnumerator ThornSpearThrowRecover()
            {
                _anim.Play("Attack Recover");
                
                yield return new WaitForSeconds(1.0f);
            }
            Control.InsertCoroutine("Thorn Spear Throw Recover", 0, ThornSpearThrowRecover);

            IEnumerator ThornSpearThrowDestroySpear()
            {
                Destroy(_thornSpear, 1.0f);

                yield return null;
            }
            Control.InsertCoroutine("Thorn Spear Throw Destroy Spear", 0, ThornSpearThrowDestroySpear);
            
            Control.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Thorn Spear Throw Antic", 0.33f, 1);
        }

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
        
        private void Awake()
        {
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _audio = gameObject.GetComponent<AudioSource>();
            Control = gameObject.LocateMyFSM("Mantis");
            _gpzControl = TraitorGod.PreloadedGameObjects["GPZ"].LocateMyFSM("Control");
            _hm = gameObject.GetComponent<HealthManager>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            
            if (PlayerData.instance.statueStateTraitorLord.usingAltVersion)
            {
                _hm.hp = TotalHealth;
                _hm.OnDeath += DeathHandler;
            }
        }

        private void ChangeStateValues()
        {
            // Increase range of Slash attack
            Control.Fsm.GetFsmFloat("Attack Speed").Value = 50.0f;
            
            // Increase vertical velocity of DSlash jump
            Control.GetAction<SetVelocity2d>("Jump").y = 40;

            // Remove the cooldown between attacksh
            Control.RemoveAction<Wait>("Cooldown");
            Control.RemoveAction<Wait>("Sick Throw CD");

            // Decrease duration of slam attack
            Control.GetAction<Wait>("Waves").time.Value = 0.0f;

            // Evenly distribute Slash and DSlash attacks and raise max attack repeats
            Control.GetAction<SendRandomEventV2>("Attack Choice").weights[0].Value = 0.33f;    // Slash weight
            Control.GetAction<SendRandomEventV2>("Attack Choice").weights[1].Value = 0.33f;    // DSlash weight
            Control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[0].Value = 4;      // Slash max repeats
            Control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[1].Value = 4;      // DSlash max repeats

            // Double the speed of the waves created by slam attack
            Control.GetAction<SetVelocity2d>("Waves", 2).x = 24; // Right Wave
            Control.GetAction<SetVelocity2d>("Waves", 7).x = -24; // Left Wave

            // Traitor God can perform slam attack at 2/3 health
            Control.GetAction<IntCompare>("Slam?").integer2 = Phase2Health + Phase3Health;

            // Make Traitor God back up for the slam at a further distance to compensate for faster waves
            Control.GetAction<FloatCompare>("Too Close?").float2 = 15.0f;

            // Set contact damage to 2
            Control.GetAction<SetDamageHeroAmount>("Land").damageDealt = 2;
            Control.GetAction<SetDamageHeroAmount>("Attack Recover").damageDealt = 2;
            
            // Fall into the arena faster
            Control.GetAction<SetVelocity2d>("Fall").y = -60;
        }
        
        /* Change wave sprite sheet using index of TraitorGod.SPRITES variable */
        private void ChangeWaveSprite(int spriteIndex)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(position);
            GameObject wave = Instantiate(Control.GetAction<SpawnObjectFromGlobalPool>("Waves").gameObject.Value,
                position, rotation);
            // slash_core is the child GameObject containing the wave's SpriteRenderer
            GameObject slashCore = wave.FindGameObjectInChildren("slash_core");
            byte[] spriteSheetByteData = TraitorGod.SpriteBytes[spriteIndex];
            slashCore.GetComponent<SpriteRenderer>().sprite.texture.LoadImage(spriteSheetByteData);
            Destroy(wave);
        }
        
        private void ClearGameObjectList(List<GameObject> gameObjectList)
        {
            foreach (GameObject go in gameObjectList)
            {
                Destroy(go);
            }
            gameObjectList.Clear();
        }

        /* Change color of waves using Mola's waves sprite sheet */
        private void ColorizeSlamWaves()
        {
            ChangeWaveSprite(1);
        }
        
        private void DeathHandler()
        {
            StartCoroutine(RectractThornPillarsAndDestroy());
        }

        /* Always target the player on DSlash */
        private void DSlashTargetPlayer()
        {
            Vector2 dSlashVector = GetVectorToPlayer() * _dSlashSpeed;;

            Control.GetAction<SetVelocity2d>("DSlash").x = dSlashVector.x;
            Control.GetAction<SetVelocity2d>("DSlash").y = dSlashVector.y;
        }

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
        
        /* Spawn shockwaves on either side */
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
                    GameObject shockwave = Instantiate(_gpzControl.GetAction<SpawnObjectFromGlobalPool>("Land Waves")
                        .gameObject.Value);
                    PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                    shockFSM.transform.localScale = new Vector2(height, width);
                    shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = @bool;
                    shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                    shockwave.AddComponent<DamageHero>().damageDealt = damage;
                    GameObject plane = shockwave.FindGameObjectInChildren("Plane");

                    plane.GetComponent<MeshRenderer>().material.mainTexture = TraitorGod.Sprites[1].texture;
                    shockwave.SetActive(true);
                    shockwave.transform.SetPosition2D(new Vector2(pos.x, 28.1f));
                }
            };
        }

        /* Spawn sickles during slash */
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
                    GameObject sickle = Instantiate(Control
                        .GetAction<SpawnObjectFromGlobalPool>("Sickle Throw")
                        .gameObject.Value, pos.SetY(sicklePosY), angle);
                        
                    sickle.GetComponent<Rigidbody2D>().velocity = new Vector2(x, 0);
                    Destroy(sickle.GetComponent<AudioSource>());
                    Destroy(sickle.GetComponent<PlayMakerFSM>());
                    Destroy(sickle, 2.0f);
                }
            };
        }

        private void SpawnWaves(float speed, float timeToLive)
        {
            float[] velocities = {-speed, speed};
            Vector2 pos = transform.position;
            Vector3 spawnPos = new Vector3(pos.x, pos.y - 5, 6.4f);
            Quaternion rot = Quaternion.Euler(0, 0, 0); ;
            
            foreach (float velocity in velocities)
            {
                GameObject wave = Instantiate(Control.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value, spawnPos, rot);
                wave.GetComponent<Rigidbody2D>().velocity = new Vector2(velocity, 0);
                GameObject slashCore = wave.FindGameObjectInChildren("slash_core");
                slashCore.GetComponent<SpriteRenderer>().flipX = (velocity < 0);
                Destroy(wave, timeToLive);
            }
        }

        private IEnumerator RectractThornPillarsAndDestroy()
        {
            SetThornPillarVelocity(120);
            yield return new WaitForSeconds(1.0f);
            ClearGameObjectList(thornsList);
        }

        private void ResetOldValues()
        {
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = TraitorGod.Sprites[3].texture;
            ChangeWaveSprite(4);
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
                ResetOldValues();
                
                yield break;
            }

                _trail = AddTrail(gameObject, 1.8f, infectionOrange);

            // Disable empty walks
            Control.RemoveTransition("Feint?", "Feint");

            // Disable slam repeat
            Control.RemoveTransition("Check L", "Repeat?");
            Control.RemoveTransition("Check R", "Repeat?");
            Control.RemoveTransition("Repeat?", "Repeat");
            Control.RemoveTransition("Repeat", "Too Close?");

            // Summon two sickles when slashing
            Control.InsertMethod("Attack Swipe", 0, SpawnSickles(40f));
            // Target the player during DSlash
            Control.InsertMethod("DSlash Antic", 0, DSlashTargetPlayer);
            // Spawn shockwaves when landing after a DSlash
            Control.InsertMethod("Land", 0, SpawnShockwaves(1, 1f, 25, 2));

            // Loop DSlash animation
            _anim.GetClipByName("DSlash").wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;
            
            AddGroundPound();
            AddWallFall();

            Log("Changing to New Sprites");
            // Change sprites using Mola's Traitor God sprite sheet
            gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture =
                TraitorGod.Sprites[0].texture;
            ColorizeSlamWaves();

            GetAudioClips();
            ChangeStateValues();
        }

        private void Update()
        {
            if (PlayerData.instance.statueStateTraitorLord.usingAltVersion)
            {
                /* Hacky method of dividing fight into 3 phases */
                if (_hm.hp < (Phase2Health + Phase3Health) && !_enteredPhase2)
                {
                    _enteredPhase2 = true;
                    AddDoubleSlam();
                }

                if (_hm.hp < Phase3Health && !_enteredPhase3)
                {
                    _enteredPhase3 = true;
                    AddThornSpearThrow();
                    AddThornPillars();
                }
            }
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
        
        /* Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/Prince.cs */
        private static ParticleSystem AddTrail(GameObject go, float offset = 0, Color? c = null)
        {
            var trail = go.AddComponent<ParticleSystem>();
            var rend = trail.GetComponent<ParticleSystemRenderer>();

            rend.material = rend.trailMaterial = new Material(Shader.Find("Particles/Additive (Soft)"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = c ?? Color.white
            };

            ParticleSystem.MainModule main = trail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startSize = 4;
            main.startLifetime = .8f;
            main.startColor = c ?? Color.white;
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