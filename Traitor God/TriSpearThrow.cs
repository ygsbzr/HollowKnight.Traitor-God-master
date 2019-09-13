using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Traitor_God
{
    public class TriSpearThrow : MonoBehaviour
    {
        private static readonly int markerSpeed = 250;
        private static readonly int spearThrowSpeed = 75;

        /* Return vector from current position of Traitor God to the current position of the Knight */
        public static Vector2 GetVectorToPlayer(Transform trans)
        {
            Vector2 heroPos = HeroController.instance.transform.position;
            Vector2 traitorPos = trans.position;

            Vector2 distanceVector = new Vector2(heroPos.x - traitorPos.x, heroPos.y - traitorPos.y);
            distanceVector.Normalize();
            /*_distanceVector.x += Math.Sign(_distanceVector.x) * 
                                 (_distanceVector.x > 0 ? Math.Sign(Herofsmler.instance.GetComponent<Rigidbody2D>().velocity.x) : 
                                     -Math.Sign(Herofsmler.instance.GetComponent<Rigidbody2D>().velocity.x)) * 0.35f;*/
            return distanceVector;
        }

        /* Throw vine-wrapped mantis spear */
        static float[] _angles;
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

                Vector2 pos = trans.position;
                foreach (float _angle in _angles)
                {
                    /* Marker attributes */
                    Quaternion rot = Quaternion.identity;
                    GameObject marker = Instantiate(TraitorGod.PreloadedGameObjects["Spear"], pos, rot);
                    marker.GetComponent<SpriteRenderer>().sprite = TraitorGod.Sprites[5];
                    marker.SetActive(true);
                    marker.AddComponent<Rigidbody2D>().isKinematic = true;
                    marker.AddComponent<NonBouncer>();
                    float x = (float)Math.Cos((_angle - 90) / Mathf.Rad2Deg);
                    float y = (float)Math.Sin((_angle - 90) / Mathf.Rad2Deg);
                    Vector2 markerVelocity = new Vector2(x, y) * markerSpeed;
                    marker.GetComponent<Rigidbody2D>().velocity = markerVelocity;
                    Trail.AddTrail(marker, 1, 1, 0.1f, 0, 0, Traitor.infectionOrange);
                    Destroy(marker, 5);
                }

                yield return new WaitForSeconds(1.0f);
            }
            fsm.InsertCoroutine("Tri-Spear Throw Antic", 0, TriSpearThrowAntic);

            IEnumerator TriSpearThrow()
            {
                anim.Play("Sickle Throw Attack");

                Vector2 pos = trans.position;
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
                    Vector2 spearVelocity = new Vector2(x, y) * spearThrowSpeed;
                    spear.GetComponent<Rigidbody2D>().velocity = spearVelocity;
                    Trail.AddTrail(spear, 2, 0.25f, 0.5f, 2, 0, Traitor.infectionOrange);
                    Destroy(spear, 5);
                }

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

        private static void Log(object message) => TraitorFinder.Log(message);
    }
}
