using System;
using IPA;
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony;
using IPALogger = IPA.Logging.Logger;
namespace SongLoaderPlugin
{
    public class Plugin : IBeatSaberPlugin
    {
        public const string VersionNumber = "7.0.0";
        public static BS_Utils.Utilities.Config ModPrefs = new BS_Utils.Utilities.Config("SongLoader");
        internal static HarmonyInstance harmony;
        private SceneEvents _sceneEvents;
        public static IPALogger logger;
        public string Name
        {
            get { return "Song Loader Plugin"; }
        } 

        public string Version
        {
            get { return VersionNumber; }
        }

        public void OnApplicationStart()
        {
            _sceneEvents = new GameObject("menu-signal").AddComponent<SceneEvents>();
            _sceneEvents.MenuSceneEnabled += OnMenuSceneEnabled;
            harmony = HarmonyInstance.Create("com.xyoncio.BeatSaber.SongLoaderPlugin");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            SongLoader.GetIcons();
      ///      SongLoader.RegisterCustomCharacteristic(SongLoader.MissingCharIcon, "Missing Characteristic", "Missing Characteristic", "MissingCharacteristic", "MissingCharacteristic");
     ///       SongLoader.RegisterCustomCharacteristic(SongLoader.LightshowIcon, "Lightshow", "Lightshow", "Lightshow", "Lightshow");
     //       SongLoader.RegisterCustomCharacteristic(SongLoader.ExtraDiffsIcon, "Lawless", "Lawless - These difficulties don't follow conventional standards, and should not necessarily be expected to reflect their given names.", "Lawless", "Lawless");


        }

        public void Init(object thisIsNull, IPALogger pluginLogger)
        {

            logger = pluginLogger;
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
          

        }

        private void OnMenuSceneEnabled()
        {
            SongLoader.OnLoad();
        }

        public void OnApplicationQuit()
        {
            PlayerPrefs.DeleteKey("lbPatched");
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {

        }

        public void OnSceneUnloaded(Scene scene)
        {
       
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        
        }
    }
}