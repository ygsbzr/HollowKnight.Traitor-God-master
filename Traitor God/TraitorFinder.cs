using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        private void SceneChanged(Scene previousScene, Scene nextScene)
        {
            /* Passing the strings instead of the Scenes because that's all we use and
             Unity kills the prev scene's name after <1 frame */
            StartCoroutine(SceneChangedRoutine(previousScene.name, nextScene.name));
        }

        private IEnumerator SceneChangedRoutine(string prev, string next)
        {
            yield return null;
            
            if (next == "GG_Workshop") SetStatue();
            
            if (next != "GG_Traitor_Lord") yield break;
            if (prev != "GG_Workshop") yield break;

            StartCoroutine(AddComponent());
        }

        private void SetStatue()
        {
            Log("Setting up statues...");

            GameObject statue = GameObject.Find("GG_Statue_TraitorLord");

            BossScene scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Traitor_Lord";

            BossStatue bs = statue.GetComponent<BossStatue>();
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = "statueStateTraitor";

            /* 56's code { */

            bs.SetPlaquesVisible(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen || bs.isAlwaysUnlocked);

            Destroy(statue.FindGameObjectInChildren("StatueAlt"));

            GameObject displayStatue = bs.statueDisplay;

            GameObject alt = Instantiate
            (
                displayStatue,
                displayStatue.transform.parent,
                true
            );
            alt.SetActive(bs.UsingDreamVersion);
            alt.GetComponentInChildren<SpriteRenderer>(true).flipX = true;
            alt.name = "StatueAlt";
            bs.statueDisplayAlt = alt;

            /* } 56's code */

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