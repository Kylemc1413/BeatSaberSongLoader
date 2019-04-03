using System;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using CustomUI.GameplaySettings;
using Harmony;
namespace SongLoaderPlugin
{
    public class Plugin : IPlugin
    {
        public const string VersionNumber = "6.10.0";
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
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            harmony = HarmonyInstance.Create("com.xyoncio.BeatSaber.SongLoaderPlugin");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            SongLoader.GetIcons();
            SongLoader.RegisterCustomCharacteristic(SongLoader.MissingCharIcon, "Missing Characteristic", "Missing Characteristic", "MissingCharacteristic", "MissingCharacteristic");

        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
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
    }
}