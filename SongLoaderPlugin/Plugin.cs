using System;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using CustomUI.Settings;
namespace SongLoaderPlugin
{
	public class Plugin : IPlugin
	{
		public const string VersionNumber = "v5.0.2-beta";

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
            
		}

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if(scene.name == "Menu")
            {
                var subMenuCC = SettingsUI.CreateSubMenu("Songloader");

                var colorOverrideOption= subMenuCC.AddBool("Allow Custom Song Colors");
                colorOverrideOption.GetValue += delegate { return ModPrefs.GetBool("Songloader", "customSongColors", true, true); };
                colorOverrideOption.SetValue += delegate (bool value) { ModPrefs.SetBool("Songloader", "customSongColors", value); };
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