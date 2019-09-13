using HutongGames.PlayMaker.Actions;

namespace Traitor_God
{
    public class ChangeStateValues
    {
        /* Change the values of various FSM Actions */
        public static void ChangeFSMValues()
        {
            /* Increase range of Slash attack */
            Traitor.Control.Fsm.GetFsmFloat("Attack Speed").Value = 50.0f;

            /* Increase vertical velocity of DSlash jump */
            Traitor.Control.GetAction<SetVelocity2d>("Jump").y = 40;

            /* Remove cooldown between attacks */
            Traitor.Control.RemoveAction<Wait>("Cooldown");
            Traitor.Control.RemoveAction<Wait>("Sick Throw CD");

            /* Decrease duration of slam attack */
            Traitor.Control.GetAction<Wait>("Waves").time.Value = 0.0f;

            /* Evenly distribute Slash and DSlash attacks and raise max attack repeats */
            Traitor.Control.GetAction<SendRandomEventV2>("Attack Choice").weights[0].Value = 0.33f;    // Slash weight
            Traitor.Control.GetAction<SendRandomEventV2>("Attack Choice").weights[1].Value = 0.33f;    // DSlash weight
            Traitor.Control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[0].Value = 4;      // Slash max repeats
            Traitor.Control.GetAction<SendRandomEventV2>("Attack Choice").eventMax[1].Value = 4;      // DSlash max repeats

            /* Double the speed of the waves created by slam attack */
            Traitor.Control.GetAction<SetVelocity2d>("Waves", 2).x = 24; // Right Wave
            Traitor.Control.GetAction<SetVelocity2d>("Waves", 7).x = -24; // Left Wave

            /* Traitor God can perform slam attack at 2/3 health */
            Traitor.Control.GetAction<IntCompare>("Slam?").integer2 = Traitor.Phase2Health + Traitor.Phase3Health;

            /* Back up for slam at a further distance to compensate for faster waves */
            Traitor.Control.GetAction<FloatCompare>("Too Close?").float2 = 15.0f;

            /* Set contact damage to 2 */
            Traitor.Control.GetAction<SetDamageHeroAmount>("Land").damageDealt = 2;
            Traitor.Control.GetAction<SetDamageHeroAmount>("Attack Recover").damageDealt = 2;

            /* Fall into the arena faster */
            Traitor.Control.GetAction<SetVelocity2d>("Fall").y = -60;
        }

        private static void Log(object message) => TraitorFinder.Log(message);
    }
}
