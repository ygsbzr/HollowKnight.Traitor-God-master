using System;
using Modding;
using UnityEngine;

namespace Traitor_God
{
    [Serializable]
    public class SaveSettings : ModSettings, ISerializationCallbackReceiver
    {
        public BossStatue.Completion completion = new BossStatue.Completion
        {
            isUnlocked = true
        };

        public bool AltStatue
        {
            get => GetBool();
            set => SetBool(value);
        }

        public void OnBeforeSerialize()
        {
            StringValues["Completion"] = JsonUtility.ToJson(completion);
        }

        public void OnAfterDeserialize()
        {
            StringValues.TryGetValue("Completion", out string @out);

            if (string.IsNullOrEmpty(@out)) return;

            completion = JsonUtility.FromJson<BossStatue.Completion>(@out);
        }
    }
}