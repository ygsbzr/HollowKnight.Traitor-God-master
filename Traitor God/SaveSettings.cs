using System;
using Modding;
using UnityEngine;

// Taken from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/SaveSettings.cs

namespace Traitor_God
{
    [Serializable]
    public class SaveSettings
    {
        public BossStatue.Completion completion = new BossStatue.Completion
        {
            isUnlocked = true
        };

        public bool AltStatue;
       
    }
}