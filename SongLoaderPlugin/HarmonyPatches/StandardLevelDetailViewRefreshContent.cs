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
        static void Postfix(ref LevelParamsPanel ____levelParamsPanel, ref IDifficultyBeatmap ____difficultyBeatmap, ref IPlayer ____player, ref TextMeshProUGUI ____songNameText)
        {
            IBeatmapLevel level = ____difficultyBeatmap.level;
            CustomLevel.CustomDifficultyBeatmap beatmap = ____difficultyBeatmap as CustomLevel.CustomDifficultyBeatmap;
            Console.WriteLine("Beatmap Extra:   " + beatmap.HasExtraLanes);
            if (beatmap.HasExtraLanes)
            {
            ____songNameText.overflowMode = TextOverflowModes.Overflow;
            ____songNameText.enableWordWrapping = false;
            ____songNameText.richText = true;
            ____songNameText.text += "<size=75%>\r\n <#FFD42A> Has Extra Lanes ";

            }


        }
    }
}
