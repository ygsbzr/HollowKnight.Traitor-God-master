using System;
using System.Collections;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bounds = UnityEngine.Bounds;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

// Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/PrinceFinder.cs

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

        private void SetStatue()
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
            Log("Statue pos before set: " + statueDisplayAlt.transform.position);

            Vector3 pos = new Vector3(188.5f, 9.6f, 0.9f);
            
            GameObject ggStatue = statue.FindGameObjectInChildren("GG_statues_0006_5");
            Sprite traitorLordStatueSprite = ggStatue.GetComponent<SpriteRenderer>().sprite;
            SpriteRenderer spriteRenderer = statueDisplayAlt.AddComponent<SpriteRenderer>();
            Log("SR position before: " + spriteRenderer.transform.position);
            Sprite sprite = spriteRenderer.sprite = traitorLordStatueSprite;
            Log("Sprite rect pos: " + sprite.rect.position);
            statueDisplayAlt.transform.localPosition = new Vector3(0.3f, 3.2f, 0.0f);
            statueDisplayAlt.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Log("Statue pos after set: " + statueDisplayAlt.transform.position);

            GameObject baseStatue = statue.FindGameObjectInChildren("Statue");
            Log("baseStatue pos: " + baseStatue.transform.position);

            GameObject statueAlt = statue.FindGameObjectInChildren("StatueAlt");
            Log("Base/StatueAlt pos: " + statueAlt.transform.position);

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