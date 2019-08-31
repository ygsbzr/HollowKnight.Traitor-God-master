using System;
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

        private void SceneChanged(Scene previousScene, Scene currentScene)
        {
            if (currentScene.name == "GG_Workshop") SetStatue();
            if (currentScene.name != "GG_Traitor_Lord") return;
            if (previousScene.name != "GG_Workshop") return;

            StartCoroutine(AddComponent());
        }

        private static void SetStatue()
        {
            GameObject statue = GameObject.Find("GG_Statue_TraitorLord");

            BossScene scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Traitor_Lord";

            BossStatue bs = statue.GetComponent<BossStatue>();
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = "statueStateTraitor";

            BossStatue.BossUIDetails details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "Traitor_Name";
            details.descriptionKey = details.descriptionSheet = "Traitor_Desc";
            bs.dreamBossDetails = details;
            
            GameObject altLever = statue.FindGameObjectInChildren("alt_lever");
            altLever.SetActive(true);
            altLever.transform.position = new Vector3(190.0f, 7.5f, 0.9f);

            GameObject switchBracket = altLever.FindGameObjectInChildren("GG_statue_switch_bracket");
            switchBracket.SetActive(true);

            GameObject switchLever = altLever.FindGameObjectInChildren("GG_statue_switch_lever");
            switchLever.SetActive(true);

            GameObject statueDisplayAlt = bs.statueDisplayAlt;
            statueDisplayAlt.SetActive(true);

            GameObject ggStatue = statue.FindGameObjectInChildren("GG_statues_0006_5");
            Sprite traitorLordStatueSprite = ggStatue.GetComponent<SpriteRenderer>().sprite;
            statueDisplayAlt.AddComponent<SpriteRenderer>().sprite = traitorLordStatueSprite;
            Vector3 statueDisplayAltPos = statueDisplayAlt.transform.position;
            statueDisplayAltPos = new Vector3(188.5f, 12f, 0.9f);
            Vector3 statueDisplayAltSpriteRendererPos = statueDisplayAlt.GetComponent<SpriteRenderer>().transform.position;
            statueDisplayAltSpriteRendererPos = new Vector3(188.5f, 12f, 0.9f);
            statueDisplayAlt.transform.localRotation = Quaternion.Euler(0, 180, 0);

            BossStatueLever toggle = statue.GetComponentInChildren<BossStatueLever>();
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