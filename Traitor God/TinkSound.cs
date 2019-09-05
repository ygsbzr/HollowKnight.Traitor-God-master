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
    public class TinkSound : MonoBehaviour
    {

        private float _nextTime;
        
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Nail Attack") || Time.time < _nextTime)
            {
                return;
            }
            _nextTime = Time.time + 0.25f;

            float degrees = 0f;
            PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(collision.gameObject, "damages_enemy");
            if (damagesEnemy != null)
            {
                degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value;
            }
            Vector3 position = HeroController.instance.transform.position;
            Vector3 euler = Vector3.zero;
            switch (DirectionUtils.GetCardinalDirection(degrees))
            {
                case 0:
                    HeroController.instance.RecoilLeft();
                    position = new Vector3(position.x + 2, position.y, 0.002f);
                    break;
                case 1:
                    HeroController.instance.RecoilDown();
                    position = new Vector3(position.x, position.y + 2, 0.002f);
                    euler = new Vector3(0, 0, 90);
                    break;
                case 2:
                    HeroController.instance.RecoilRight();
                    position = new Vector3(position.x - 2, position.y, 0.002f);
                    euler = new Vector3(0, 0, 180);
                    break;
                default:
                    position = new Vector3(position.x, position.y - 2, 0.002f);
                    euler = new Vector3(0, 0, 270);
                    break;
            }
            GameObject effect = Instantiate(
                Traitor.Control.GetAction<SpawnObjectFromGlobalPool>("Sickle Throw").gameObject.Value
                    .GetComponent<TinkEffect>().blockEffect);
            effect.transform.localPosition = position;
            effect.transform.localRotation = Quaternion.Euler(euler);
            effect.SetActive(true);
            }
    }
}