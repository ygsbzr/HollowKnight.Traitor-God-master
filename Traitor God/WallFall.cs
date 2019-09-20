using System;
using System.Collections;
using HutongGames.PlayMaker;
using UnityEngine;

namespace Traitor_God
{
    public class WallFall
    {
        private static readonly int wallFallVelocity = 60;
        private static readonly float yFloor = 31.6f;

        /* Fall down when encountering wall or ceiling */
        public static void AddWallFall(PlayMakerFSM fsm, 
                                       tk2dSpriteAnimator anim, 
                                       Rigidbody2D rb, 
                                       Transform trans)
        {
            fsm.CreateState("Wall Fall");

            fsm.AddTransition("DSlash", "HITWALL", "Wall Fall");
            fsm.AddTransition("Wall Fall", FsmEvent.Finished, "Land");

            IEnumerator WallFall()
            {
                anim.Play("Enter");
                rb.velocity = Vector2.down * wallFallVelocity;

                while (trans.position.y > yFloor)
                {
                    yield return null;
                }
                fsm.SetState("Land");

                yield return null;
            }

            fsm.InsertCoroutine("Wall Fall", 0, WallFall);
        }

        private static void Log(object message) => Modding.Logger.Log($"[Wall Fall]: " + message);
    }
}
