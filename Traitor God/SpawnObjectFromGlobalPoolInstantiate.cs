using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Traitor_God
{
    public class SpawnObjectFromGlobalPoolInstantiate : RigidBody2dActionBase
    {
        public override void Reset()
        {
            gameObject = null;
        }
        
        public Func<GameObject> gameObject;
    }
}