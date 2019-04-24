using System;
using IPA;
using UnityEngine;
using UnityEngine.SceneManagement;
using CustomUI.GameplaySettings;
using Harmony;
namespace SongLoaderPlugin
{
    public class Plugin : IBeatSaberPlugin
    {
        public const string VersionNumber = "6.12.4";
        public static BS_Utils.Utilities.Config ModPrefs = new BS_Utils.Utilities.Config("SongLoader");
        internal static HarmonyInstance harmony;
        private SceneEvents _sceneEvents;

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
            SongLoader.RegisterCustomCharacteristic(SongLoader.MissingCharIcon, "Missing Characteristic", "Missing Characteristic", "MissingCharacteristic", "MissingCharacteristic");
            SongLoader.RegisterCustomCharacteristic(SongLoader.LightshowIcon, "Lightshow", "Lightshow", "Lightshow", "Lightshow");
            SongLoader.RegisterCustomCharacteristic(SongLoader.ExtraDiffsIcon, "Lawless", "Lawless - These difficulties don't follow conventional standards, and should not necessarily be expected to reflect their given names.", "Lawless", "Lawless");

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
            if (scene.name == "MenuViewControllers")
            {

                if (SongLoader.reqDialog == null)
                    SongLoader.InitRequirementsMenu();

                var subMenuCC = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.PlayerSettingsLeft, "Song Loader", "MainMenu",
                    "songloader", "Songloader Options");

                var colorOverrideOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.PlayerSettingsLeft, "Allow Custom Song Colors",
                    "songloader", "Allow Custom Songs to override note/light colors if Custom Colors or Chroma is installed");
                colorOverrideOption.GetValue = ModPrefs.GetBool("Songloader", "customSongColors", true, true);
                colorOverrideOption.OnToggle += delegate (bool value) { ModPrefs.SetBool("Songloader", "customSongColors", value); };

                var platformOverrideOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.PlayerSettingsLeft, "Allow Custom Song Platforms",
                    "songloader", "Allow Custom Songs to override your Custom Platform if Custom Platforms is installed");
                platformOverrideOption.GetValue = ModPrefs.GetBool("Songloader", "customSongPlatforms", true, true);
                platformOverrideOption.OnToggle += delegate (bool value) { ModPrefs.SetBool("Songloader", "customSongPlatforms", value); };

            }
        }

        public void OnSceneUnloaded(Scene scene)
        {
       
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        
        }
    }
}