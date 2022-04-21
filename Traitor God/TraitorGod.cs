using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;

// Taken and modified from https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/PalePrince.cs

namespace Traitor_God
{
    [UsedImplicitly]
    public class TraitorGod : Mod, ITogglableMod,ILocalSettings<SaveSettings>
    {
        public static readonly List<Sprite> Sprites = new List<Sprite>();
        public static readonly List<byte[]> SpriteBytes = new List<byte[]>();

        public static Shader FlashShader;
        
        [PublicAPI]
        public static TraitorGod Instance { get; private set; }

        public static readonly Dictionary<string, GameObject> PreloadedGameObjects = new Dictionary<string, GameObject>();
        
        public override string GetVersion()
        {
            return Assembly.GetAssembly(typeof(TraitorGod)).GetName().Version.ToString();
        }

        private string _previousScene;

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Grey_Prince_Zote", "Grey Prince"),
                ("Fungus3_11", "fungd_spikes_09_FG 7"),
                ("Fungus3_11", "fungd_spikes_09_FG"),
                ("Fungus3_11", "fungd_spike_sil_04"),
                ("GG_Ghost_No_Eyes_V", "fungd_spikes_0_0001_d"),
                ("GG_Soul_Master", "Mage Lord"),
            };
        }
        public static SaveSettings Settings = new();
        public void OnLoadLocal(SaveSettings s)=>Settings = s;
        public SaveSettings OnSaveLocal() => Settings;
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GameObjects");
            PreloadedGameObjects.Add("GPZ", preloadedObjects["GG_Grey_Prince_Zote"]["Grey Prince"]);
            PreloadedGameObjects.Add("Thorns Left", preloadedObjects["Fungus3_11"]["fungd_spikes_09_FG 7"]);
            PreloadedGameObjects.Add("Thorns Right", preloadedObjects["Fungus3_11"]["fungd_spikes_09_FG"]);
            PreloadedGameObjects.Add("Black Thorns", preloadedObjects["Fungus3_11"]["fungd_spike_sil_04"]);
            PreloadedGameObjects.Add("Thorn Point", preloadedObjects["GG_Ghost_No_Eyes_V"]["fungd_spikes_0_0001_d"]);
            PreloadedGameObjects.Add("Soul Master", preloadedObjects["GG_Soul_Master"]["Mage Lord"]);

            Instance = this;
            
            Log("Initializing...");

            Unload();

            ModHooks.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.SavegameSaveHook += SaveGameSave;
            On.HeroController.Start += AddComponent;
            ModHooks.LanguageGetHook += OnLangGet;
            ModHooks.SetPlayerVariableHook += SetVariableHook;
            ModHooks.GetPlayerVariableHook += GetVariableHook;
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
                    SpriteBytes.Add(buffer);
                    Sprites.Add(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f)));

                    Log("Created sprite from embedded image: " + resource + " at index " + index);
                    index++;
                }
            }

            Log("Initialized.");
        }

        private void AddComponent(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            SaveGameSave();
            if(GameManager.instance.gameObject.GetComponent<TraitorFinder>()==null)
            {
                GameManager.instance.gameObject.AddComponent<TraitorFinder>();
            }
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

        private void SceneChanged(Scene previousScene, Scene currentScene)
        {
            _previousScene = previousScene.name;
        }

        private string OnLangGet(string key, string sheettitle,string orig)
        {
            /*string text = Language.Language.GetInternal(key, sheettitle);
            Log("Key: " + key);
            Log("Text: " + text);
            return text;*/
            
            switch (key)
            {
                case "Traitor_Name":
                    return "Traitor God";
                case "TRAITOR_LORD_MAIN" when _previousScene == "GG_Workshop" && PlayerData.instance.statueStateTraitorLord.usingAltVersion:
                    return "Traitor";
                case "TRAITOR_LORD_SUB" when _previousScene == "GG_Workshop" && PlayerData.instance.statueStateTraitorLord.usingAltVersion:
                    return "God";
                case "Traitor_Desc":
                    return "Renegade god of corruption";
                default:
                    return orig;
            }
        }

        private void BeforeSaveGameSave(SaveGameData data)
        {
            Settings.AltStatue = PlayerData.instance.statueStateTraitorLord.usingAltVersion;

            PlayerData.instance.statueStateTraitorLord.usingAltVersion = false;
        }

        private void SaveGameSave(int id = 0)
        {
            PlayerData.instance.statueStateTraitorLord.usingAltVersion = Settings.AltStatue;
        }


        public void Unload()
        {
            ModHooks.BeforeSavegameSaveHook -= BeforeSaveGameSave;
            ModHooks.SavegameSaveHook -= SaveGameSave;
            On.HeroController.Start -= AddComponent;
            ModHooks.LanguageGetHook -= OnLangGet;
            ModHooks.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.GetPlayerVariableHook -= GetVariableHook;
            USceneManager.activeSceneChanged -= SceneChanged;

            TraitorFinder finder = GameManager.instance.gameObject.GetComponent<TraitorFinder>();

            if (finder != null)
                UObject.Destroy(finder);
        }
    }
}