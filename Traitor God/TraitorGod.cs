using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Modding;
using ModCommon;
using MonoMod.RuntimeDetour;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;

// Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/PalePrince.cs

namespace Traitor_God
{
    [UsedImplicitly]
    public class TraitorGod : Mod<SaveSettings>, ITogglableMod
    {
        
        public static readonly List<Sprite> SPRITES = new List<Sprite>();
        public static readonly List<byte[]> SPRITEBYTE = new List<byte[]>();
        
        [PublicAPI]
        public static TraitorGod Instance { get; private set; }

        public static Dictionary<string, GameObject> preloadedGameObjects = new Dictionary<string, GameObject>();
        
        public override string GetVersion()
        {
            return Assembly.GetAssembly(typeof(TraitorGod)).GetName().Version.ToString();
        }

        private string _lastScene;

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Grey_Prince_Zote", "Grey Prince"),
                ("Fungus3_11", "fungd_spikes_09_FG 7"),
                ("Fungus3_11", "fungd_spikes_09_FG"),
                ("Fungus3_11", "fungd_spike_sil_04"),
                ("GG_Ghost_No_Eyes_V", "fungd_spikes_0_0001_d"),
            };
        }
        
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GameObjects");
            preloadedGameObjects.Add("GPZ", preloadedObjects["GG_Grey_Prince_Zote"]["Grey Prince"]);
            preloadedGameObjects.Add("ThornsL", preloadedObjects["Fungus3_11"]["fungd_spikes_09_FG 7"]);
            preloadedGameObjects.Add("ThornsR", preloadedObjects["Fungus3_11"]["fungd_spikes_09_FG"]);
            preloadedGameObjects.Add("BlackThorns", preloadedObjects["Fungus3_11"]["fungd_spike_sil_04"]);
            preloadedGameObjects.Add("ThornPoint", preloadedObjects["GG_Ghost_No_Eyes_V"]["fungd_spikes_0_0001_d"]);

            Instance = this;
            
            Log("Initializing...");

            Unload();

            ModHooks.Instance.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.SavegameSaveHook += SaveGameSave;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += OnLangGet;
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;
            USceneManager.activeSceneChanged += SceneChanged;
            
            // Taken from https://github.com/SalehAce1/PaleChampion/blob/master/PaleChampion/PaleChampion/PaleChampion.cs
            int index = 0;
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in assembly.GetManifestResourceNames())
            {
                if (!resource.EndsWith(".png"))
                {
                    continue;
                }
                
                using (Stream stream = assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null) continue;

                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    stream.Dispose();

                    // Create texture from bytes
                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(buffer, true);
                    // Create sprite from texture
                    SPRITEBYTE.Add(buffer);
                    SPRITES.Add(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f)));

                    Log("Created sprite from embedded image: " + resource + " at index " + index);
                    index++;
                }
            }
                        
            Log("Initialized.");
        }

        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStateTraitor")
                Settings.completion = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            return key == "statueStateTraitor"
                ? Settings.completion
                : orig;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            _lastScene = arg0.name;
        }

        private string OnLangGet(string key, string sheettitle)
        {
            /*string text = Language.Language.GetInternal(key, sheettitle);
            Log("Key: " + key);
            Log("Text: " + text);
            return text;*/
            
            switch (key)
            {
                case "Traitor_Name":
                    return "Traitor God";
                case "TRAITOR_LORD_MAIN" when _lastScene == "GG_Workshop" && PlayerData.instance.statueStateTraitorLord.usingAltVersion:
                    return "Traitor";
                case "TRAITOR_LORD_SUB" when _lastScene == "GG_Workshop" && PlayerData.instance.statueStateTraitorLord.usingAltVersion:
                    return "God";
                case "Traitor_Desc":
                    return "Renegade god of fury";
                default:
                    return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void BeforeSaveGameSave(SaveGameData data)
        {
            Settings.AltStatue = PlayerData.instance.statueStateTraitorLord.usingAltVersion;

            PlayerData.instance.statueStateTraitorLord.usingAltVersion = false;
        }

        private void SaveGame(SaveGameData data)
        {
            SaveGameSave();
            AddComponent();
        }

        private void SaveGameSave(int id = 0)
        {
            PlayerData.instance.statueStateTraitorLord.usingAltVersion = Settings.AltStatue;
        }

        private static void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<TraitorFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.BeforeSavegameSaveHook -= BeforeSaveGameSave;
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.SavegameSaveHook -= SaveGameSave;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= OnLangGet;
            ModHooks.Instance.SetPlayerVariableHook -= SetVariableHook;
            USceneManager.activeSceneChanged -= SceneChanged;

            TraitorFinder finder = GameManager.instance.gameObject.GetComponent<TraitorFinder>();

            if (finder != null)
                UObject.Destroy(finder);
        }
    }
}