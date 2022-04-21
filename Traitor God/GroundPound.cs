using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Traitor_God
{
    public class GroundPound : MonoBehaviour
    {
        private static int jumpVelocity = 50;
        private static int groundPoundVelocity = 60;

        /* Spawn shockwaves on either side */
        public static Action SpawnShockwaves(Transform trans, 
                                             PlayMakerFSM shockwaveFSM, 
                                             float width, 
                                             float height, 
                                             float speed, 
                                             int damage)
        {
            return () =>
            {
                Quaternion angle = Quaternion.identity;
                Vector3 pos = trans.position;

                bool[] facingRightBools = { false, true };

                foreach (bool @bool in facingRightBools)
                {
                    GameObject shockwave = Instantiate(shockwaveFSM.GetAction<SpawnObjectFromGlobalPool>("Land Waves")
                    .gameObject.Value);
                    PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                    shockFSM.transform.localScale = new Vector2(height, width);
                    shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = @bool;
                    shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                    shockwave.AddComponent<DamageHero>().damageDealt = damage;
#if DEBUG
                    shockwave.AddComponent<DebugColliders>();
#endif
                    shockwave.SetActive(true);
                    shockwave.transform.SetPosition2D(new Vector2(pos.x, 28.1f));
                }
            };
        }

        /* Vertical ground pound, indicated by a red trail */
        public static void AddGroundPound(PlayMakerFSM fsm, 
                                          PlayMakerFSM shockwaveFSM, 
                                          ParticleSystem trail, 
                                          tk2dSpriteAnimator anim, 
                                          Rigidbody2D rb, 
                                          Transform trans)
        {
            string[] states =
            {
                "Ground Pound Jump Antic",
                "Ground Pound Jump",
                "Ground Pound Fall",
                "Ground Pound Land",
                "Ground Pound Recover",
            };

            fsm.CreateStates(states);

            Traitor.Audio.time = 0.15f;

            /* Telegraph ground pound with lower pitched DSlash growl */
            IEnumerator GroundPoundJumpAntic()
            {
                Log("Ground Pound Jump Antic");
                ParticleSystem.MainModule main = trail.main;
                main.startColor = Color.red;

                anim.Play("Jump Antic");
                Traitor.Audio.pitch = 0.9f;
                TraitorAudio.PlayAudioClip("Jump Antic");
                rb.velocity = Vector2.zero;

                yield return new WaitForSeconds(0.25f);
            }
            fsm.InsertCoroutine("Ground Pound Jump Antic", 0, GroundPoundJumpAntic);

            /* Set jump velocity */
            IEnumerator GroundPoundJump()
            {
                Log("Ground Pound Jump");
                anim.Play("Jump");
                TraitorAudio.PlayAudioClip("Jump");
                rb.velocity = Vector2.up * jumpVelocity;

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Ground Pound Jump", 0, GroundPoundJump);

            /* Set ground pound fall velocity */
            IEnumerator GroundPoundFall()
            {
                Log("Ground Pound Fall");
                anim.Play("DSlash");
                TraitorAudio.PlayAudioClip("DSlash");
                trans.rotation = Quaternion.Euler(0, 0, -Math.Sign(trans.localScale.x));
                rb.velocity =Vector2.down * groundPoundVelocity;
                while (trans.position.y > 32)
                {
                    yield return null;
                }
            }
            fsm.InsertCoroutine("Ground Pound Fall", 0, GroundPoundFall);

            /* Land and generate taller shockwaves */
            IEnumerator GroundPoundLand()
            {
                Log("Ground Pound Land");
                anim.Play("Land");
                GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
                rb.velocity = Vector2.zero;
                trans.rotation = Quaternion.identity;
                TraitorAudio.PlayAudioClip("Land");

                yield return null;
            }
            fsm.InsertCoroutine("Ground Pound Land", 0, GroundPoundLand);

            fsm.InsertMethod("Ground Pound Land", 0, SpawnShockwaves(trans, shockwaveFSM, 2, 2, 75, 2));

            /* Revert back to orange trail */
            IEnumerator GroundPoundRecover()
            {
                Log("Ground Pound Recover");
                ParticleSystem.MainModule main = trail.main;
                main.startColor = Traitor.InfectionOrange;

                yield return new WaitForSeconds(1.0f);
            }
            fsm.InsertCoroutine("Ground Pound Recover", 0, GroundPoundRecover);


            fsm.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Ground Pound Jump Antic", 0.33f, 1);
        }

        private static void Log(object message) => Modding.Logger.Log($"[Ground Pound]: " + message);
    }
}
