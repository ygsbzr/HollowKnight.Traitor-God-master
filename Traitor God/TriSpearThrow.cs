using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HutongGames.PlayMaker.Actions;
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
        public static Dictionary<GameObject, float> SpearDict = new Dictionary<GameObject, float>();
        public static void AddTriSpearThrow(PlayMakerFSM fsm, tk2dSpriteAnimator anim, Rigidbody2D rb, Transform trans)
        {
            string[] states =
            {
                "Tri-Spear Throw Backstep",
                "Tri-Spear Throw Antic",
                "Tri-Spear Throw",
                "Tri-Spear Throw Recover",
            };

            fsm.CreateStates(states);

            IEnumerator TriSpearThrowBackstep()
            {
                Traitor.Audio.pitch = 1.0f;
                Traitor.Audio.time = 0.25f;
                TraitorAudio.PlayAudioClip("Slash Antic");
                anim.Play("Walk");
                rb.velocity = new Vector2(-trans.localScale.x * 15, 0);
                yield return new WaitForSeconds(0.3f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw Backstep", 0, TriSpearThrowBackstep);
            
            IEnumerator TriSpearThrowAntic()
            {
                Log("Tri-Spear Throw Antic");
                anim.Play("Sickle Throw Antic");
                TraitorAudio.PlayAudioClip("Teleport");
                rb.velocity = Vector2.zero;

                Vector2 vectorToTarget = GetVectorToPlayer(trans);

                float angle = (float)Math.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;
                Log("Angle: " + angle);
                float[] _angles = {
                    angle - 30, 
                    angle,
                    angle + 30
                };

                Vector2 pos = trans.position;
                foreach (float _angle in _angles)
                {
                    Log("Creating spear");
                    /* Spear attributes */
                    Quaternion rot = Quaternion.Euler(0, 0, _angle);
                    GameObject spear = Instantiate(new GameObject("Traitor Spear"), pos, rot);
                    spear.AddComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[2];
                    spear.SetActive(true);
                    spear.layer = 11;
                    Rigidbody2D spearRb = spear.AddComponent<Rigidbody2D>();
                    spearRb.isKinematic = true;
                    Trail.AddTrail(spear, 2, 0.25f, 0.5f, 2, 0, Traitor.InfectionOrange);
                    SpearDict.Add(spear, _angle);

                    Destroy(spear, 5);
                    var keys = SpearDict.Keys;
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
                Log("Tri-Spear Throw");
                anim.Play("Sickle Throw Attack");

                foreach (KeyValuePair<GameObject, float> entry in SpearDict)
                {
                    Log("Getting spear");
                    GameObject spear = entry.Key;
                    if (spear != null)
                    {
                        float angle = entry.Value;
                        spear.GetComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[3];
                        BoxCollider2D spearCollider = spear.AddComponent<BoxCollider2D>();
# if DEBUG
                        spear.AddComponent<DebugColliders>();
# endif
                        spear.AddComponent<TinkEffect>();
                        spear.AddComponent<TinkSound>();
                        spearCollider.size = new Vector2(12, 0.5f);
                        spearCollider.offset = new Vector2(0, 0.3f);
                        spear.AddComponent<DamageHero>().damageDealt = 2;
                        float x = (float) Math.Cos(angle / Mathf.Rad2Deg);
                        float y = (float) Math.Sin(angle / Mathf.Rad2Deg);
                        Vector2 spearVelocity = new Vector2(x, y) * spearThrowSpeed;
                        Rigidbody2D spearRb = spear.GetComponent<Rigidbody2D>();
                        spearRb.velocity = spearVelocity;
                    }
                }
                SpearDict = new Dictionary<GameObject, float>();
                yield return new WaitForSeconds(0.25f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw", 0, TriSpearThrow);

            IEnumerator TriSpearThrowRecover()
            {
                Log("Tri-Spear Throw Recover");
                anim.Play("Attack Recover");

                yield return new WaitForSeconds(1.0f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw Recover", 0, TriSpearThrowRecover);

            fsm.GetAction<SendRandomEventV2>("Attack Choice").AddToSendRandomEventV2("Tri-Spear Throw Backstep", 0.33f, 1);
        }

        private static void Log(object message) => Modding.Logger.Log($"[Tri-Spear Throw]: " + message);
    }
}
