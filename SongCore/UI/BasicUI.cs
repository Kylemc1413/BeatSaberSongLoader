﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomUI.GameplaySettings;
using CustomUI.Settings;
using UnityEngine;
using CustomUI.BeatSaber;
namespace SongCore.UI
{
    internal static class BasicUI
    {
        internal static BS_Utils.Utilities.Config ModPrefs = new BS_Utils.Utilities.Config("SongCore");
        internal static UnityEngine.UI.Button infoButton;
        internal static CustomUI.BeatSaber.CustomMenu reqDialog;
        internal static CustomUI.BeatSaber.CustomListViewController reqViewController;
        internal static Sprite HaveReqIcon;
        internal static Sprite MissingReqIcon;
        internal static Sprite HaveSuggestionIcon;
        internal static Sprite MissingSuggestionIcon;
        internal static Sprite WarningIcon;
        internal static Sprite InfoIcon;
        //    internal static Sprite CustomSongsIcon;
   //     internal static Sprite MissingCharIcon;
   //     internal static Sprite LightshowIcon;
   //     internal static Sprite ExtraDiffsIcon;



        public static void CreateUI()
        {
                var songCoreSubMenu = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.PlayerSettingsLeft, "SongCore", "MainMenu",
    "songcore", "SongCore Options");

            var colorOverrideOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.PlayerSettingsLeft, "Allow Custom Song Colors",
                "songcore", "Allow Custom Songs to override note/light colors if Custom Colors or Chroma is installed");
            colorOverrideOption.GetValue = ModPrefs.GetBool("SongCore", "customSongColors", true, true);
            colorOverrideOption.OnToggle += delegate (bool value) { Plugin.customSongColors = value; ModPrefs.SetBool("SongCore", "customSongColors", value); };

            var platformOverrideOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.PlayerSettingsLeft, "Allow Custom Song Platforms",
                "songcore", "Allow Custom Songs to override your Custom Platform if Custom Platforms is installed");
            platformOverrideOption.GetValue = ModPrefs.GetBool("SongCore", "customSongPlatforms", true, true);
            platformOverrideOption.OnToggle += delegate (bool value) { Plugin.customSongPlatforms = value; ModPrefs.SetBool("SongCore", "customSongPlatforms", value); };


        }




        internal static void InitRequirementsMenu()
        {
            reqDialog = BeatSaberUI.CreateCustomMenu<CustomMenu>("Additional Song Information");
            reqViewController = BeatSaberUI.CreateViewController<CustomListViewController>();
            RectTransform confirmContainer = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
            confirmContainer.SetParent(reqViewController.rectTransform, false);
            confirmContainer.sizeDelta = new Vector2(60f, 0f);
            GetIcons();
            reqDialog.SetMainViewController(reqViewController, true);


        }

        internal static void showSongRequirements(Data.ExtraSongData songData, Data.ExtraSongData.DifficultyData diffData, bool wipFolder)
        {
            //   suggestionsList.text = "";

            reqViewController.Data.Clear();
            //Contributors
            if (songData.contributors.Count() > 0)
            {
                foreach (Data.ExtraSongData.Contributor author in songData.contributors)
                {
                    if (author.icon == null)
                        if (!string.IsNullOrWhiteSpace(author.iconPath))
                        {
                            author.icon = Utilities.Utils.LoadSpriteFromFile(songData.songPath + "/" + author.iconPath);
                            reqViewController.Data.Add(new CustomCellInfo(author.name, author.role, author.icon));
                        }
                        else
                            reqViewController.Data.Add(new CustomCellInfo(author.name, author.role, InfoIcon));
                    else
                        reqViewController.Data.Add(new CustomCellInfo(author.name, author.role, author.icon));
                }
            }
            //WIP Check
            if (wipFolder)
                reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + "WIP", "Warning", WarningIcon));
            //Additional Diff Info
            if(diffData != null)
            {
                if (diffData.additionalDifficultyData.requirements.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData.requirements)
                    {
                        //    Console.WriteLine(req);
                        if (!Collections.capabilities.Contains(req))
                            reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Missing Requirement", MissingReqIcon));
                        else
                            reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Requirement", HaveReqIcon));
                    }
                }

                if (diffData.additionalDifficultyData.warnings.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData.warnings)
                    {

                        //    Console.WriteLine(req);

                        reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Warning", WarningIcon));
                    }
                }
                if (diffData.additionalDifficultyData.information.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData.information)
                    {

                        //    Console.WriteLine(req);

                        reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Info", InfoIcon));
                    }
                }
                if (diffData.additionalDifficultyData.suggestions.Count() > 0)
                {
                    foreach (string req in diffData.additionalDifficultyData.suggestions)
                    {

                        //    Console.WriteLine(req);
                        if (!Collections.capabilities.Contains(req))
                            reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Missing Suggestion", MissingSuggestionIcon));
                        else
                            reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Suggestion", HaveSuggestionIcon));
                    }
                }
            }
         



            reqDialog.Present();
            reqViewController._customListTableView.ReloadData();

        }
        internal static void GetIcons()
        {
            //      if (!CustomSongsIcon)
            //           CustomSongsIcon = Utilities.Utils.LoadSpriteFromResources("SongLoaderPlugin.Icons.CustomSongs.png");
            if (!MissingReqIcon)
                MissingReqIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.RedX.png");
            if (!HaveReqIcon)
                HaveReqIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.GreenCheck.png");
            if (!HaveSuggestionIcon)
                HaveSuggestionIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.YellowCheck.png");
            if (!MissingSuggestionIcon)
                MissingSuggestionIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.YellowX.png");
            if (!WarningIcon)
                WarningIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.Warning.png");
            if (!InfoIcon)
                InfoIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.Info.png");
     //       if (!MissingCharIcon)
     //           MissingCharIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.MissingChar.png");
     //       if (!LightshowIcon)
     //           LightshowIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.Lightshow.png");
     //       if (!ExtraDiffsIcon)
     //           ExtraDiffsIcon = Utilities.Utils.LoadSpriteFromResources("SongCore.Icons.ExtraDiffsIcon.png");

        }


    }
}
