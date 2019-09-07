using System.Collections;
using System.Reflection;
using ModCommon;
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

        private void SceneChanged(Scene previousScene, Scene currentScene) => StartCoroutine(SceneChangedRoutine(previousScene, currentScene));
        
        private IEnumerator SceneChangedRoutine(Scene prev, Scene next)
        {
            yield return null;
            
            if (next.name == "GG_Workshop") SetStatue();
            if (next.name != "GG_Traitor_Lord") yield break;
            if (prev.name != "GG_Workshop") yield break;

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
            
            bs.SetPlaquesVisible(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen || bs.isAlwaysUnlocked);

            Destroy(statue.FindGameObjectInChildren("StatueAlt"));

            GameObject displayStatue = bs.statueDisplay;

            GameObject alt = Instantiate
            (
                displayStatue,
                displayStatue.transform.parent,
                true
            );

            // FUCK local rotation
            alt.GetComponentInChildren<SpriteRenderer>(true).flipX = true;

            alt.name = "StatueAlt";

            bs.statueDisplayAlt = alt;

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

            bs.OnStatueSwapFinished += () =>
            {
                if (bs.UsingDreamVersion)
                {
                    StartCoroutine(RaiseStatue(alt));
                }
            };
        }

        private IEnumerator RaiseStatue(GameObject alt)
        {
            for (int i = 0; i < 50; i++)
            {
                alt.transform.position += new Vector3(0, .0195f);
                
                yield return new WaitForSeconds(0.002f);
            }
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