using GlobalEnums;
using UnityEngine;

//Taken from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/DamageOnCollision.cs

namespace Traitor_God {
    internal class DamageOnCollision : MonoBehaviour
    {
        public int Damage { private get; set; } = 1;

        private void OnParticleCollision(GameObject other)
        {
            if (other != HeroController.instance.gameObject) return;
            HeroController.instance.TakeDamage(gameObject, CollisionSide.other, Damage, 1);
        }
    }
}