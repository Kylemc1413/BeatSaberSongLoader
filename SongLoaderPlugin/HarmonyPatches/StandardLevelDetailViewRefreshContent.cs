using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using TMPro;
using SongLoaderPlugin.OverrideClasses;
namespace SongLoaderPlugin.Harmony_Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailViewController))]
    [HarmonyPatch("RefreshContent", MethodType.Normal)]

    class StandardLevelDetailViewRefreshContent
    {
        static void Postfix(ref LevelParamsPanel ____levelParamsPanel, ref IDifficultyBeatmap ____difficultyBeatmap,
            ref IPlayer ____player, ref TextMeshProUGUI ____songNameText, ref UnityEngine.UI.Button ____playButton, ref UnityEngine.UI.Button ____practiceButton)
        {
            IBeatmapLevel level = ____difficultyBeatmap.level;
            CustomLevel.CustomDifficultyBeatmap beatmap = ____difficultyBeatmap as CustomLevel.CustomDifficultyBeatmap;
            ____playButton.interactable = true;
            ____practiceButton.interactable = true;
            ____songNameText.overflowMode = TextOverflowModes.Overflow;
            ____songNameText.enableWordWrapping = false;
            ____songNameText.richText = true;
            SongLoader.currentRequirements = beatmap.requirements;
            ____songNameText.text += "<size=75%>\r\n <#FFD42A> Requirements: ";
            foreach(string req in beatmap.requirements)
            {
                Console.WriteLine(req);
                if (!SongLoader.capabilities.Contains(req))
                {
                ____songNameText.text += "<#FF0000>" + req + "<#FFD42A> | ";
                ____playButton.interactable = false;
                    ____practiceButton.interactable = false;
                }
                else
                {
                    ____songNameText.text += "<#FFD42A>" + req + " | ";
                }


            }



        }
    }
}
