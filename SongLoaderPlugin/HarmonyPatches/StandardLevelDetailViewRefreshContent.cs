using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using TMPro;
using SongLoaderPlugin.OverrideClasses;
using UnityEngine.UI;
//using CustomUI.BeatSaber;
namespace SongLoaderPlugin.Harmony_Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailViewController))]
    [HarmonyPatch("RefreshContent", MethodType.Normal)]

    class StandardLevelDetailViewRefreshContent
    {
        static void Postfix(ref LevelParamsPanel ____levelParamsPanel, ref IDifficultyBeatmap ____difficultyBeatmap,
            ref IPlayer ____player, ref TextMeshProUGUI ____songNameText, ref UnityEngine.UI.Button ____playButton, ref UnityEngine.UI.Button ____practiceButton)
        {
            IBeatmapLevel level = ____difficultyBeatmap?.level;
           
            ____playButton.interactable = true;
            ____practiceButton.interactable = true;
            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = new Color(0, 0.706f, 1.000f, 0.784f);
            ____songNameText.overflowMode = TextOverflowModes.Overflow;
            ____songNameText.enableWordWrapping = false;
            ____songNameText.richText = true;
            if (level != null)
            {
                var customLevel = level as CustomLevel;

                CustomLevel.CustomDifficultyBeatmap beatmap = ____difficultyBeatmap as CustomLevel.CustomDifficultyBeatmap;
                if (SongLoader.infoButton == null)
                {
                    SongLoader.infoButton = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PlayButton")), (RectTransform)____playButton.transform.parent.transform, false);
                //    SongLoader.infoButton.SetButtonText("?");
                    //   SongLoader.infoButton = CustomUI.BeatSaber.BeatSaberUI.CreateUIButton((RectTransform)____playButton.transform.parent.transform, "PlayButton", null, "?");
                    SongLoader.infoButton.GetComponentInChildren<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
                    (SongLoader.infoButton.transform as RectTransform).sizeDelta = new Vector2(0.1f, 0.1f);
                }



                if (beatmap != null)
                {
                    SongLoader.infoButton.onClick.RemoveAllListeners();
                    SongLoader.infoButton.onClick.AddListener(delegate ()
                    {
                        if (beatmap != null)
                            SongLoader.showSongRequirements(beatmap, customLevel.customSongInfo);
                    });
                    if (beatmap.requirements.Count == 0 && beatmap.suggestions.Count == 0 && beatmap.warnings.Count == 0 && 
                        customLevel?.customSongInfo?.mappers?.Length == 0 && customLevel?.customSongInfo?.lighters?.Length == 0 && beatmap.information.Count == 0)
                    {
                        SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.black;
                        SongLoader.infoButton.interactable = false;
                    }
                    else if (beatmap.warnings.Count == 0)
                    {
                        SongLoader.infoButton.interactable = true;
                        SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.yellow;
                    }
                    else if(beatmap.warnings.Count > 0)
                    {
                        SongLoader.infoButton.interactable = true;
                        SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                        if (beatmap.warnings.Contains("WIP"))
                        {
                            ____playButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                        }
                    }


                    SongLoader.currentRequirements = beatmap.requirements;
                    SongLoader.currentSuggestions = beatmap.suggestions;
                    
                    for (int i = 0; i < beatmap.requirements.Count; i++)
                    {
                        if (!SongLoader.capabilities.Contains(beatmap.requirements[i]))
                        {
                            ____playButton.interactable = false;
                            ____practiceButton.interactable = false;
                            ____playButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.red;
                            SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = new Color(0, 0.706f, 1.000f, 0.784f);
                        }
                    }



                }
                else
                {
                    SongLoader.infoButton.gameObject.GetComponentInChildren<UnityEngine.UI.Image>().color = Color.black;
                    SongLoader.infoButton.interactable = false;
                }


            }


        }
    }
}

