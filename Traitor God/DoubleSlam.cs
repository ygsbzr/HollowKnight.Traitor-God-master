using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Traitor_God
{
    public class DoubleSlam : MonoBehaviour
    {
        /* Two successive slam waves */
        public static void AddDoubleSlam(PlayMakerFSM fsm, 
                                         tk2dSpriteAnimator anim, 
                                         Transform trans)
        {
            string[] states =
            {
                "Double Slam Antic",
                "Double Slam Slamming",
                "Double Slam Waves 1",
                "Double Slam Waves 2",
            };

            fsm.CreateStates(states);

            /* Helper function for spawning slam waves */
            void SpawnWaves(float speed, float timeToLive)
            {
                Vector2 pos = trans.position;
                float[] velocities = { -speed, speed };
                Vector3 spawnPos = new Vector3(pos.x, pos.y - 5, 6.4f);
                Quaternion rot = Quaternion.identity;

                foreach (float velocity in velocities)
                {
                    GameObject wave = Instantiate(fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value, spawnPos, rot);
                    wave.GetComponent<Rigidbody2D>().velocity = Vector2.right * velocity;
                    wave.GetComponentInChildren<SpriteRenderer>().flipX = velocity < 0;
                    Destroy(wave, timeToLive);
                }
            }

            /* Telegraph for slam */
            IEnumerator DoubleSlamAntic()
            {
                anim.Play("Shockwave Antic");
                Traitor.Audio.pitch = 0.9f;
                TraitorAudio.PlayAudioClip("Roar");

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Double Slam Antic", 0, DoubleSlamAntic);

            /* During slam */
            IEnumerator DoubleSlamSlamming()
            {
                anim.Play("Shockwave Attack");
                TraitorAudio.PlayAudioClip("Slamming");
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Double Slam Slamming", 0, DoubleSlamSlamming);

            /* First pair of waves */
            IEnumerator DoubleSlamWaves1()
            {
                TraitorAudio.PlayAudioClip("Slamming");
                SpawnWaves(12, 3);

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Double Slam Waves 1", 0, DoubleSlamWaves1);

            /* Second pair of waves */
            IEnumerator DoubleSlamWaves2()
            {
                SpawnWaves(24, 3);

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Double Slam Waves 2", 0, DoubleSlamWaves2);

            fsm.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("Double Slam Antic", 0.2f, 2);
        }

        private static void Log(object message) => Modding.Logger.Log($"[Double Slam]: " + message);
    }
}
