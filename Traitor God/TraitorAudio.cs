using System;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Traitor_God
{
    public class TraitorAudio
    {
        /* Assign values of audio clips used in custom attacks */
        private static AudioClip GetAudioClip(string clipName)
        {
            switch (clipName)
            {
                case "DSlash":
                case "Jump Antic":
                case "Jump":
                case "Land":
                case "Roar":
                    return Traitor.Control.GetAction<AudioPlayerOneShot>(clipName).audioClips[0];
                case "Slash Antic":
                    return Traitor.Control.GetAction<AudioPlayerOneShot>("Attack Antic").audioClips[0];
                case "Slash":
                    return Traitor.Control.GetAction<AudioPlayerOneShot>("Attack 1").audioClips[0];
                case "Slamming":
                    return (AudioClip)Traitor.Control.GetAction<AudioPlayerOneShotSingle>(clipName).audioClip.Value;
                default:
                    return null;
            }
        }

        public static void PlayAudioClip(string clipName)
        {
            AudioClip audioClip = GetAudioClip(clipName);
            Traitor.Audio.PlayOneShot(audioClip);
        }

        private static void Log(object message) => TraitorFinder.Log(message);
    }
}
