using System.Collections;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Traitor_God
{
    internal class TraitorFinder : MonoBehaviour
    {

        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg1.name == "GG_Workshop") SetStatue();
            if (arg1.name != "GG_Traitor_Lord") return;
            if (arg0.name != "GG_Workshop") return;

            StartCoroutine(AddComponent());
        }

        private static void SetStatue()
        {
            GameObject statue = GameObject.Find("GG_Statue_TraitorLord");

            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Traitor_Lord";

            var bs = statue.GetComponent<BossStatue>();
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = "statueStateTraitor";

            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "Traitor_Name";
            details.descriptionKey = details.descriptionSheet = "Traitor_Desc";
            bs.dreamBossDetails = details;

            GameObject @switch = statue.FindGameObjectInChildren("dream_version_switch");
            @switch.SetActive(true);
            @switch.transform.position = new Vector3(189.0f, 6.5f, 0.8f);

            GameObject burst = @switch.FindGameObjectInChildren("Burst Pt");
            burst.transform.position = new Vector3(188.4f, 4.9f, 0.8f);

            GameObject glow = @switch.FindGameObjectInChildren("Base Glow");
            glow.transform.position = new Vector3(188.4f, 5.9f, 3.0f);

            glow.GetComponent<tk2dSprite>().color = Color.white;

            var fader = glow.GetComponent<ColorFader>();
            fader.upColour = Color.black;
            fader.downColour = Color.white;

            var toggle = statue.GetComponentInChildren<BossStatueDreamToggle>();
            toggle.SetOwner(bs);
            toggle.SetState(true);
        }

        private static IEnumerator AddComponent()
        {
            yield return null;

            GameObject.Find("Mantis Traitor Lord").AddComponent<Traitor>();
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }

        public static void Log(object o)
        {
            Logger.Log($"[{Assembly.GetExecutingAssembly().GetName().Name}]: " + o);
        }
    }
}