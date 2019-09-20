using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using UnityEngine;

namespace Traitor_God
{
    public class TriSpearThrow : MonoBehaviour
    {
        private static readonly int spearThrowSpeed = 75;

        /* Return vector from current position of Traitor God to the current position of the Knight */
        public static Vector2 GetVectorToPlayer(Transform trans)
        {
            Vector2 heroPos = HeroController.instance.transform.position;
            Vector2 traitorPos = trans.position;

            Vector2 distanceVector = new Vector2(heroPos.x - traitorPos.x, heroPos.y - traitorPos.y);
            distanceVector.Normalize();
            return distanceVector;
        }

        /* Throw vine-wrapped mantis spear */
        static float[] _angles;
        static Dictionary<GameObject, float> spearDict = new Dictionary<GameObject, float>();
        public static void AddTriSpearThrow(PlayMakerFSM fsm, tk2dSpriteAnimator anim, Rigidbody2D rb, Transform trans)
        {
            string[] states =
            {
                "Tri-Spear Throw Antic",
                "Tri-Spear Throw",
                "Tri-Spear Throw Recover",
            };

            fsm.CreateStates(states);

            Vector2 vectorToTarget = new Vector2();
            Vector2 pos = new Vector2();

            IEnumerator TriSpearThrowAntic()
            {
                anim.Play("Sickle Throw Antic");
                Traitor.Audio.pitch = 0.9f;
                TraitorAudio.PlayAudioClip("Slash Antic");
                rb.velocity = Vector2.zero;

                vectorToTarget = GetVectorToPlayer(trans);

                // + 90 degrees because spear sprite is facing down and must be rotated to align with 0 degrees Cartesian
                float angle = (float)Math.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg + 90;
                Log("Angle: " + angle);
                _angles = new float[] {
                        angle - 30,
                        angle,
                        angle + 30
                };

                pos = trans.position;
                foreach (float _angle in _angles)
                {
                    Log("Creating spear");
                    /* Spear attributes */
                    Quaternion rot = Quaternion.Euler(0, 0, _angle);
                    GameObject spear = Instantiate(TraitorGod.PreloadedGameObjects["Spear"], pos, rot);
                    spear.GetComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[2];
                    spear.SetActive(true);
                    spear.layer = 11;
                    spear.AddComponent<Rigidbody2D>().isKinematic = true;
                    Trail.AddTrail(spear, 2, 0.25f, 0.5f, 2, 0, Traitor.infectionOrange);
                    spearDict.Add(spear, _angle);

                    Destroy(spear, 5);
                    var keys = spearDict.Keys;
                    foreach (var key in keys)
                    {
                        Log("Key: " + key);
                    }
                }

                yield return new WaitForSeconds(1.0f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw Antic", 0, TriSpearThrowAntic);

            IEnumerator TriSpearThrow()
            {
                anim.Play("Sickle Throw Attack");

                foreach (KeyValuePair<GameObject, float> entry in spearDict)
                {
                    Log("Getting spear");
                    GameObject spear = entry.Key;
                    float angle = entry.Value;
                    BoxCollider2D spearCollider = spear.AddComponent<BoxCollider2D>();
# if DEBUG
                    spear.AddComponent<DebugColliders>();
# endif
                    spear.AddComponent<TinkEffect>();
                    spear.AddComponent<TinkSound>();
                    spearCollider.size = new Vector2(1, 12);
                    spear.AddComponent<DamageHero>().damageDealt = 2;
                    float x = (float)Math.Cos((angle - 90) / Mathf.Rad2Deg);
                    float y = (float)Math.Sin((angle - 90) / Mathf.Rad2Deg);
                    Vector2 spearVelocity = new Vector2(x, y) * spearThrowSpeed;
                    spear.GetComponent<Rigidbody2D>().velocity = spearVelocity;
                }
                spearDict = new Dictionary<GameObject, float>();

                yield return new WaitForSeconds(0.25f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw", 0, TriSpearThrow);

            IEnumerator TriSpearThrowRecover()
            {
                anim.Play("Attack Recover");

                yield return new WaitForSeconds(1.0f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw Recover", 0, TriSpearThrowRecover);

            fsm.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Tri-Spear Throw Antic", 0.33f, 1);
        }

        private static void Log(object message) => Modding.Logger.Log($"[Tri-Spear Throw]: " + message);
    }
}
