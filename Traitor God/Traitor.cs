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

            _control.Fsm.GetFsmFloat("Attack Speed").Value = 0.25f;
            _control.Fsm.GetFsmFloat("DSlash Speed").Value = 0.25f;
            
            _hm.hp = HP;   
        }
    }
}