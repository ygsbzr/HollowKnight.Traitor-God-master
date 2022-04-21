using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Traitor_God
{
    public class ThornPillars : MonoBehaviour
    {
        public static void SetThornPillarVelocity(float y)
        {
            foreach (GameObject thorns in thornsList)
            {
                if (thorns != null)
                {
                    Rigidbody2D rb = thorns.GetComponent<Rigidbody2D>();
                    rb.isKinematic = true;
                    rb.velocity = Vector2.up * y;
                }
            }
        }

        /* Retract all existing thorn pillars and destroy them */
        public static IEnumerator RetractThornPillarsAndDestroy()
        {
            SetThornPillarVelocity(120);
            yield return new WaitForSeconds(1.0f);
            Traitor.ClearGameObjectList(thornsList);
        }

        /* AOE thorn pillars */
        public static List<GameObject> thornsList = new List<GameObject>();   // List containing all thorn GameObjects
        public static void AddThornPillars(PlayMakerFSM fsm, 
                                           tk2dSpriteAnimator anim, 
                                           Transform trans)
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

            fsm.CreateStates(states);

            /* Spawn thorn pillars and move them slightly into view at the top of the arena */
            IEnumerator ThornPillarsAppear()
            {
                Log("Thorn Pillars Appear");
                Traitor.Audio.time = 0.25f;
                Traitor.Audio.pitch = 0.9f;
                TraitorAudio.PlayAudioClip("Roar");
                anim.Play("Roar");
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
                
                Vector2 pos = trans.position;
                
                /* Spawn thorn pillars on left side of Traitor God */
                for (float i = pos.x - 5; i >= 0; i -= 6)
                {
                    for (float j = 62f; j <= 105; j += 5)
                    {
                        SpawnThorns("Thorns Left", new Vector2(8, 2), new Vector3(i - 1, j, 0));
                        SpawnThorns("Black Thorns", new Vector2(5, 2), new Vector3(i, j, 0));
                        SpawnThorns("Thorns Right", new Vector2(8, 2), new Vector3(i + 1, j, 0));
                    }
                    SpawnThorns("Thorn Point", new Vector2(3, 8), new Vector3(i, 59, 0));
                }
                
                /* Spawn thorn pillars on right side of Traitor God */
                for (float i = pos.x + 5; i <= 75; i += 6)
                {
                    for (float j = 62f; j <= 105; j += 5)
                    {
                        SpawnThorns("Thorns Left", new Vector2(8, 2), new Vector3(i - 1, j, 0));
                        SpawnThorns("Black Thorns", new Vector2(5, 2), new Vector3(i, j, 0));
                        SpawnThorns("Thorns Right", new Vector2(8, 2), new Vector3(i + 1, j, 0));
                    }
                    SpawnThorns("Thorn Point", new Vector2(3, 8), new Vector3(i, 59, 0));
                }

                SetThornPillarVelocity(-60);

                yield return new WaitForSeconds(0.25f);
            }
            fsm.InsertCoroutine("Thorn Pillars Appear", 0, ThornPillarsAppear);

            /* Pause briefly to allow player to react */
            IEnumerator ThornPillarsAppearPause()
            {
                Log("Thorn Pillars Appear Pause");
                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Thorn Pillars Appear Pause", 0, ThornPillarsAppearPause);

            /* Drop thorn pillars */
            IEnumerator ThornPillarsDrop()
            {
                Log("Thorn Pillars Drop");
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                SetThornPillarVelocity(-80);

                yield return new WaitForSeconds(0.25f);
            }
            fsm.InsertCoroutine("Thorn Pillars Drop", 0, ThornPillarsDrop);

            /* Keep pillars fully dropped for half a second */
            IEnumerator ThornPillarsDropPause()
            {
                Log("Thorn Pillars Drop Pause");
                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                TraitorAudio.PlayAudioClip("Land");

                SetThornPillarVelocity(0);

                yield return new WaitForSeconds(0.5f);


            }
            fsm.InsertCoroutine("Thorn Pillars Drop Pause", 0, ThornPillarsDropPause);

            /* Retract pillars */
            IEnumerator ThornPillarsRetract()
            {
                Log("Thorn Pillars Retract");
                SetThornPillarVelocity(90);

                yield return new WaitForSeconds(0.5f);
            }
            fsm.InsertCoroutine("Thorn Pillars Retract", 0, ThornPillarsRetract);

            /* Remove thorn pillars */
            IEnumerator ThornPillarsRecover()
            {
                Log("Thorn Pillars Recover");
                Traitor.ClearGameObjectList(thornsList);
                anim.Play("Idle");

                yield return null;
            }
            fsm.InsertCoroutine("Thorn Pillars Recover", 0, ThornPillarsRecover);

            fsm.GetAction<SendRandomEventV2>("Slam?").AddToSendRandomEventV2("Thorn Pillars Appear", 0.33f, 1);
        }

        private static void Log(object message) => Modding.Logger.Log($"[Thorn Pillars]: " + message);
    }
}
