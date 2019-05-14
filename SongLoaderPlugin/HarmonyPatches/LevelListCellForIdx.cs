﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
namespace SongLoaderPlugin.HarmonyPatches
{


    [HarmonyPatch(typeof(LevelPackLevelsTableView))]
    [HarmonyPatch("CellForIdx", MethodType.Normal)]
    public class LevelListCellForIdx
    {
        static void Prefix(ref LevelPackLevelsTableView __instance, int row, ref HMUI.TableView ____tableView, ref bool ____showLevelPackHeader, ref IBeatmapLevelPack ____pack)
        {
            if (!SongLoader.AreSongsLoaded)
                return;

            if (____pack.beatmapLevelCollection.beatmapLevels.Length == 0)
                return;

            int num = ____showLevelPackHeader ? (row - 1) : row;
            num = Mathf.Clamp(num, 0, ____pack.beatmapLevelCollection.beatmapLevels.Length - 1);

            //     Console.WriteLine($"Num: {num}   Size: {____pack.beatmapLevelCollection.beatmapLevels.Length}");

            if (!(____pack.beatmapLevelCollection.beatmapLevels[num] is OverrideClasses.CustomLevel))
                return;

            OverrideClasses.CustomLevel customLevel = ____pack.beatmapLevelCollection.beatmapLevels[num] as OverrideClasses.CustomLevel;
            if (!customLevel)
                return;

            if (customLevel.coverImageTexture2D == SongLoader.CustomSongsIcon.texture)
            {
                SongLoader.LoadSprite(customLevel.customSongInfo.path + "/" + customLevel.customSongInfo.coverImagePath, customLevel);
            }
        }
    }
}
