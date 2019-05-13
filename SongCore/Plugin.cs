using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPA;
using Harmony;
using IPALogger = IPA.Logging.Logger;
using SongCore.Utilities;
using BSEvents = CustomUI.Utilities.BSEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace SongCore
{
    public class Plugin : IBeatSaberPlugin
    {
        internal static HarmonyInstance harmony;

        public void OnApplicationStart()
        {
            harmony = HarmonyInstance.Create("com.kyle1413.BeatSaber.SongCore");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            if (!File.Exists(Collections.dataPath)) File.Create(Collections.dataPath);
            Collections.Load();


        }

        public void Init(object thisIsNull, IPALogger pluginLogger)
        {

            Utilities.Logging.logger = pluginLogger;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name == "MenuCore")
                UI.BasicUI.CreateUI();
        }

        public void OnSceneUnloaded(Scene scene)
        {

        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {

            if (nextScene.name == "MenuCore")
            {
                //Code to execute when entering The Menu
                Logging.Log("Scene Changed to Menu");

            }

            if (nextScene.name == "GameCore")
            {
                //Code to execute when entering actual gameplay


            }
        }

        public void OnApplicationQuit()
        {

        }

        public void OnLevelWasLoaded(int level)
        {

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

    }
}
