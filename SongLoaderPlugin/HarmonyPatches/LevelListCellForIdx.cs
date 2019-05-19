using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using SongCore.OverrideClasses;
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

            if (!(____pack.beatmapLevelCollection.beatmapLevels[num] is CustomLevel))
                return;
            CustomLevel customLevel = ____pack.beatmapLevelCollection.beatmapLevels[num] as CustomLevel;
            if (!customLevel)
                return;

            if (customLevel.coverImageTexture2D == SongCore.UI.BasicUI.CustomSongsIcon.texture)
            {
                SongLoader.LoadSprite(customLevel.customSongInfo.customLevelPath + "/" + customLevel.customSongInfo.coverImageFilename, customLevel);
            }


        }
    }
    [HarmonyPatch(typeof(LevelPackLevelsViewController))]
    [HarmonyPatch("HandleLevelPackLevelsTableViewDidSelectLevel", MethodType.Normal)]
    class LevelPackLevelsSelectedPatch
    {
  //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
        static void Prefix(LevelPackLevelsTableView tableView, IPreviewBeatmapLevel level)
        {
            CustomLevel customLevel = level as CustomLevel;

      //      if (previouslySelectedSong != null)
      //         SongLoader.Instance.UnloadAudio(previouslySelectedSong);

            if (customLevel != null)
            {
                if (customLevel.previewAudioClip != SongLoader.TemporaryAudioClip || customLevel.AudioClipLoading) return;
                customLevel.FixBPMAndGetNoteJumpMovementSpeed();
                SongLoader.Instance.LoadAudio(
                    "file:///" + customLevel.customSongInfo.customLevelPath + "/" + customLevel.customSongInfo.songFilename, customLevel, null);
                //            previouslySelectedSong = customLevel;
                SongCore.Collections.AddSong(customLevel.customSongInfo.GetIdentifier(), customLevel.customSongInfo.customLevelPath);
                SongCore.Collections.SaveExtraSongData();
            }
        }
    }


}