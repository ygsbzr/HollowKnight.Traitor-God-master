using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using UnityEngine;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Traitor_God
{
    internal class Traitor : MonoBehaviour
    {
        private const int HP = 2500;
        private HealthManager _hm;

        private Vector2 _heroPos;
        private Vector2 _traitorPos;
        private Vector2 _dSlashVector;
        private int _dSlashSpeed = 50;

        private tk2dSpriteAnimator _anim;

        private PlayMakerFSM _control;

        private readonly Random _rand = new Random();

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _control = gameObject.LocateMyFSM("Mantis");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
        }
        
        private IEnumerator Start()
        {
            yield return null;

            while (HeroController.instance == null)
            {
                yield return null;
            }


            _control.Fsm.GetFsmFloat("Attack Speed").Value = 50.0f;

            // Remove the cooldown between attacks.
            _control.RemoveAction<Wait>("Cooldown");
            _control.RemoveAction<Wait>("Sick Throw CD");
            
                        
            // Double the speed of the waves created by slam attack
            _control.GetAction<SetVelocity2d>("Waves", 2).x = 24;    // Right Wave
            _control.GetAction<SetVelocity2d>("Waves", 7).x = -24;    // Left Wave

            // Traitor Lord can perform slam attack at 2/3 health
            _control.GetAction<IntCompare>("Slam?").integer2 = HP * 2 / 3;

            _hm.hp = HP;   
        }

        private void Update()
        {
            // Always target the player on DSlash
            _heroPos = HeroController.instance.transform.position;
            _traitorPos = transform.position;
            
            _dSlashVector = new Vector2(_heroPos.x - _traitorPos.x, _heroPos.y - _traitorPos.y);
            _dSlashVector.Normalize();
            _dSlashVector *= _dSlashSpeed;
            _control.GetAction<SetVelocity2d>("DSlash").x = _dSlashVector.x;
            _control.GetAction<SetVelocity2d>("DSlash").y = _dSlashVector.y;
        }
    }
}