using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SimpleJSON;
using SongLoaderPlugin.Internals;
using SongLoaderPlugin.OverrideClasses;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LogSeverity = IPA.Logging.Logger.Level;
namespace SongLoaderPlugin
{
    public class SongLoader : MonoBehaviour
    {
        internal static Sprite CustomSongsIcon;
        internal static Sprite MissingCharIcon;
        internal static Sprite LightshowIcon;
        internal static Sprite ExtraDiffsIcon;

        public static string standardCharacteristicName = "LEVEL_STANDARD";
        public static string oneSaberCharacteristicName = "LEVEL_ONE_SABER";
        public static string noArrowsCharacteristicName = "LEVEL_NO_ARROWS";
        public static event Action<SongLoader> LoadingStartedEvent;
        public static event Action<SongLoader, List<CustomLevel>> SongsLoadedEvent;
        public static List<CustomLevel> CustomLevels = new List<CustomLevel>();
        public static bool AreSongsLoaded { get; private set; }
        public static bool AreSongsLoading { get; private set; }
        public static float LoadingProgress { get; private set; }
        public static CustomLevelCollectionSO CustomLevelCollectionSO { get; private set; }
        public static CustomLevelCollectionSO WIPCustomLevelCollectionSO { get; private set; }
        public static CustomBeatmapLevelPackCollectionSO CustomBeatmapLevelPackCollectionSO { get; private set; }
        public static CustomBeatmapLevelPackSO CustomBeatmapLevelPackSO { get; private set; }
        public static CustomBeatmapLevelPackSO WIPCustomBeatmapLevelPackSO { get; private set; }

        public const string MenuSceneName = "MenuCore";
        public const string GameSceneName = "GameCore";

        private static readonly Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();

        private LeaderboardScoreUploader _leaderboardScoreUploader;
        private StandardLevelDetailViewController _standardLevelDetailViewController;
        internal LevelPackLevelsViewController _LevelListViewController;

        private BeatmapCharacteristicSO[] beatmapCharacteristicSOCollection;

        private readonly ScriptableObjectPool<CustomLevel> _customLevelPool = new ScriptableObjectPool<CustomLevel>();
        private readonly ScriptableObjectPool<CustomBeatmapDataSO> _beatmapDataPool = new ScriptableObjectPool<CustomBeatmapDataSO>();

        private ProgressBar _progressBar;

        private HMTask _loadingTask;
        private bool _loadingCancelled;

        public static CustomLevel.CustomDifficultyBeatmap CurrentLevelPlaying { get; private set; }

        public static readonly AudioClip TemporaryAudioClip = AudioClip.Create("temp", 1, 2, 1000, true);

        private LogSeverity _minLogSeverity;
        internal static bool firstLoad = false;
        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Song Loader").AddComponent<SongLoader>();
        }

        public static SongLoader Instance;

        private void Awake()
        {
            Instance = this;
            _minLogSeverity = Environment.CommandLine.Contains("--mute-song-loader")
                ? LogSeverity.Error
                : LogSeverity.Info;
            _progressBar = ProgressBar.Create();
            OnSceneTransitioned(SceneManager.GetActiveScene());
            RefreshSongs();
            DontDestroyOnLoad(gameObject);

            SceneEvents.Instance.SceneTransitioned += OnSceneTransitioned;
        }

        private void OnSceneTransitioned(Scene activeScene)
        {
            if (AreSongsLoading)
            {
                //Scene changing while songs are loading. Since we are using a separate thread while loading, this is bad and could cause a crash.
                //So we have to stop loading.
                if (_loadingTask != null)
                {
                    _loadingTask.Cancel();
                    _loadingCancelled = true;
                    AreSongsLoading = false;
                    LoadingProgress = 0;
                    StopAllCoroutines();
                    _progressBar.ShowMessage("Loading cancelled\n<size=80%>Press Ctrl+R to refresh</size>");
                    Log("Loading was cancelled by player since they loaded another scene.");
                }
            }

            StartCoroutine(WaitRemoveScores());
            if (activeScene.name == MenuSceneName)
            {
                BS_Utils.Gameplay.Gamemode.Init();
                CurrentLevelPlaying = null;
                if (CustomLevelCollectionSO == null)
                {
                    var levelCollectionSO = Resources.FindObjectsOfTypeAll<BeatmapLevelCollectionSO>().FirstOrDefault();
                    CustomLevelCollectionSO = CustomLevelCollectionSO.ReplaceOriginal(levelCollectionSO);
                    WIPCustomLevelCollectionSO = CustomLevelCollectionSO.ReplaceOriginal(levelCollectionSO);
                }

                if (CustomBeatmapLevelPackCollectionSO == null)
                {
                    var beatmapLevelPackCollectionSO = Resources.FindObjectsOfTypeAll<BeatmapLevelPackCollectionSO>().FirstOrDefault();
                    CustomBeatmapLevelPackCollectionSO = CustomBeatmapLevelPackCollectionSO.ReplaceOriginal(beatmapLevelPackCollectionSO);
                    CustomBeatmapLevelPackSO = CustomBeatmapLevelPackSO.GetPack(CustomLevelCollectionSO);
                    CustomBeatmapLevelPackCollectionSO.AddLevelPack(CustomBeatmapLevelPackSO);
                    WIPCustomBeatmapLevelPackSO = CustomBeatmapLevelPackSO.GetPack(WIPCustomLevelCollectionSO, true);
                    CustomBeatmapLevelPackCollectionSO.AddLevelPack(WIPCustomBeatmapLevelPackSO);
                    CustomBeatmapLevelPackCollectionSO.ReplaceReferences();
                }
                else
                {
                    CustomBeatmapLevelPackCollectionSO.ReplaceReferences();
                }
                ReloadHashes();
                /*
                var Extras = CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Where(x => x.packName.Contains("Extras")).First();
                if (Extras != null)
                {
                    BeatmapLevelSO[] ExtrasLevels = Extras.GetField<BeatmapLevelCollectionSO>("_beatmapLevelCollection").GetField<BeatmapLevelSO[]>("_beatmapLevels");
                    if (!firstLoad)
                        foreach (BeatmapLevelSO level in ExtrasLevels)
                        {
                            CustomLevelCollectionSO._levelList.Add(level);
                            firstLoad = true;
                        }
                    */
                //         var extraPack = Extras as BeatmapLevelPackSO;
                //       extraPack.SetPrivateField("_coverImage", CustomSongsIcon);
                // ReflectionUtil.SetPrivateField(Extras, "_beatmapLevelCollection", CustomLevelCollectionSO);
                //    ReloadHashes();
                //  }

                beatmapCharacteristicSOCollection = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicCollectionSO>().FirstOrDefault().beatmapCharacteristics;
                var soloFreePlay = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault();
                LevelPacksViewController levelPacksViewController = soloFreePlay.GetField<LevelPacksViewController>("_levelPacksViewController");
                levelPacksViewController.SetData(CustomBeatmapLevelPackCollectionSO, 0);
                if (_standardLevelDetailViewController == null)
                {
                    _standardLevelDetailViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().FirstOrDefault();
                    if (_standardLevelDetailViewController == null) return;
                    _standardLevelDetailViewController.didPressPlayButtonEvent += StandardLevelDetailControllerOnDidPressPlayButtonEvent;
                }

                if (_LevelListViewController == null)
                {
                    _LevelListViewController = Resources.FindObjectsOfTypeAll<LevelPackLevelsViewController>().FirstOrDefault();
                    if (_LevelListViewController == null) return;

                    _LevelListViewController.didSelectLevelEvent += StandardLevelListViewControllerOnDidSelectLevelEvent;
                }



            }
            else if (activeScene.name == GameSceneName)
            {
                GameplayCoreSceneSetupData data = BS_Utils.Plugin.LevelData?.GameplayCoreSceneSetupData;
                if (!BS_Utils.Plugin.LevelData.IsSet) return;
                var level = data.difficultyBeatmap;
                var beatmap = level as CustomLevel.CustomDifficultyBeatmap;
                if (beatmap != null)
                {
                    CurrentLevelPlaying = beatmap;

                    //The note jump movement speed now gets set in the Start method, so we're too early here. We have to wait a bit before overriding.
                    Invoke(nameof(DelayedNoteJumpMovementSpeedFix), 0.1f);
                }

                if (NoteHitVolumeChanger.PrefabFound) return;
                var song = CustomLevels.FirstOrDefault(x => x.levelID == level.level.levelID);
                if (song == null) return;
                NoteHitVolumeChanger.SetVolume(song.customSongInfo.noteHitVolume, song.customSongInfo.noteMissVolume);

            }
        }


        private void DelayedNoteJumpMovementSpeedFix()
        {
            //Beat Saber 0.11.1 introduced a check for if noteJumpMovementSpeed <= 0
            //This breaks songs that have a negative noteJumpMovementSpeed and previously required a patcher to get working again
            //I've added this to add support for that again, because why not.
            if (CurrentLevelPlaying.noteJumpMovementSpeed <= 0)
            {
                var beatmapObjectSpawnController =
                    Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>().FirstOrDefault();
                if (beatmapObjectSpawnController != null)
                {
                    var disappearingArrows = beatmapObjectSpawnController.GetPrivateField<bool>("_disappearingArrows");
                    var ghostNotes = beatmapObjectSpawnController.GetPrivateField<bool>("_ghostNotes");

                    beatmapObjectSpawnController.Init(CurrentLevelPlaying.level.beatsPerMinute,
                        CurrentLevelPlaying.beatmapData.beatmapLinesData.Length,
                        CurrentLevelPlaying.noteJumpMovementSpeed, CurrentLevelPlaying.noteJumpStartBeatOffset, disappearingArrows, ghostNotes);
                }
            }

            //Also change beatmap to no arrow if no arrow was selected, since Beat Saber no longer does runtime conversion for that.
            //As of 0.13.0 No Arrows is no longer a selectable mode, and is instead a separate set of difficultybeatmaps contained in the song
            //So for now this is obsolete
            /*
             *             if (!_noArrowsSelected) return;
            var gameplayCore = Resources.FindObjectsOfTypeAll<GameplayCoreSceneSetup>().FirstOrDefault();
            if (gameplayCore == null) return;
            Console.WriteLine("Applying no arrow transformation");
            var transformedBeatmap = BeatmapDataNoArrowsTransform.CreateTransformedData(CurrentLevelPlaying.beatmapData, true);
            var beatmapDataModel = gameplayCore.GetPrivateField<BeatmapDataModel>("_beatmapDataModel");
            beatmapDataModel.SetPrivateField("_beatmapData", transformedBeatmap);
            */
        }



        private void StandardLevelListViewControllerOnDidSelectLevelEvent(LevelPackLevelsViewController levelListViewController, IPreviewBeatmapLevel level)
        {

        }

        public void LoadAudioClipForLevel(CustomLevel customLevel, Action<CustomLevel> clipReadyCallback)
        {
            Action callback = delegate { clipReadyCallback(customLevel); };

            customLevel.FixBPMAndGetNoteJumpMovementSpeed();
            LoadAudio(
            "file:///" + customLevel.customSongInfo.path + "/" + customLevel.customSongInfo.GetAudioPath(), customLevel, callback);

        }

        private IEnumerator WaitRemoveScores()
        {
            yield return new WaitForSecondsRealtime(1f);
            RemoveCustomScores();
        }

        private void StandardLevelDetailControllerOnDidPressPlayButtonEvent(StandardLevelDetailViewController songDetailViewController)
        {
            if (!NoteHitVolumeChanger.PrefabFound) return;
            var level = songDetailViewController.selectedDifficultyBeatmap.level;
            var song = CustomLevels.FirstOrDefault(x => x.levelID == level.levelID);
            if (song == null) return;
            NoteHitVolumeChanger.SetVolume(song.customSongInfo.noteHitVolume, song.customSongInfo.noteMissVolume);
        }

        public void RefreshSongs(bool fullRefresh = true)
        {
            if (SceneManager.GetActiveScene().name != MenuSceneName) return;
            if (AreSongsLoading) return;

            Log(fullRefresh ? "Starting full song refresh" : "Starting song refresh");
            AreSongsLoaded = false;
            AreSongsLoading = true;
            LoadingProgress = 0;
            _loadingCancelled = false;

            if (LoadingStartedEvent != null)
            {
                try
                {
                    LoadingStartedEvent(this);
                }
                catch (Exception e)
                {
                    Log("Some plugin is throwing exception from the LoadingStartedEvent!", IPA.Logging.Logger.Level.Error);
                    Log(e.ToString(), IPA.Logging.Logger.Level.Error);
                }
            }

            foreach (var customLevel in CustomLevels)
            {
                if (CustomLevelCollectionSO._levelList.Contains(customLevel))
                    CustomLevelCollectionSO.RemoveLevel(customLevel);
                if (WIPCustomLevelCollectionSO._levelList.Contains(customLevel))
                    WIPCustomLevelCollectionSO.RemoveLevel(customLevel);
            }

            RetrieveAllSongs(fullRefresh);
        }

        //Use these methods if your own plugin deletes a song and you want the song loader to remove it from the list.
        //This is so you don't have to do a full refresh.
        public void RemoveSongWithPath(string path)
        {
            RemoveSong(CustomLevels.FirstOrDefault(x => x.customSongInfo.path == path));
        }

        public void RemoveSongWithLevelID(string levelID)
        {
            RemoveSong(CustomLevels.FirstOrDefault(x => x.levelID == levelID));
        }

        public void RemoveSong(IBeatmapLevel level)
        {
            if (level == null) return;
            RemoveSong(level as CustomLevel);
        }

        public void RemoveSong(CustomLevel customLevel)
        {
            if (customLevel == null) return;
            if (CustomLevelCollectionSO._levelList.Contains(customLevel))
                CustomLevelCollectionSO.RemoveLevel(customLevel);
            if (WIPCustomLevelCollectionSO._levelList.Contains(customLevel))
                WIPCustomLevelCollectionSO.RemoveLevel(customLevel);
            foreach (IDifficultyBeatmapSet beatmapset in customLevel.difficultyBeatmapSets)
                foreach (var difficultyBeatmap in beatmapset.difficultyBeatmaps)
                {
                    var customDifficulty = difficultyBeatmap as CustomLevel.CustomDifficultyBeatmap;
                    if (customDifficulty == null) continue;
                    _beatmapDataPool.Return(customDifficulty.BeatmapDataSO);
                }

            _customLevelPool.Return(customLevel);
        }

        private void RemoveCustomScores()
        {
            if (PlayerPrefs.HasKey("lbPatched")) return;
            _leaderboardScoreUploader = FindObjectOfType<LeaderboardScoreUploader>();
            if (_leaderboardScoreUploader == null) return;
            var scores =
                _leaderboardScoreUploader.GetPrivateField<List<LeaderboardScoreUploader.ScoreData>>("_scoresToUploadForCurrentPlayer");

            var scoresToRemove = new List<LeaderboardScoreUploader.ScoreData>();
            foreach (var scoreData in scores)
            {
                if (scoreData.beatmap.level is CustomLevel)
                {
                    Log("Removing a custom score here");
                    scoresToRemove.Add(scoreData);
                }
            }

            scores.RemoveAll(x => scoresToRemove.Contains(x));
        }

        internal static void LoadSprite(string spritePath, CustomLevel customLevel)
        {
            Sprite sprite;
            if (!LoadedSprites.ContainsKey(spritePath))
            {
                if (!File.Exists(spritePath))
                {
                    //Cover image doesn't exist, ignore it.
                    return;
                }

                var bytes = File.ReadAllBytes(spritePath);
                var tex = new Texture2D(256, 256);
                if (!tex.LoadImage(bytes, true))
                {
                    Instance?.Log("Failed to load cover image: " + spritePath);
                    return;
                }

                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
                LoadedSprites.Add(spritePath, sprite);
            }
            else
            {
                sprite = LoadedSprites[spritePath];
            }

            customLevel.SetCoverImage(sprite);
        }

        //     internal void UnloadAudio(CustomLevel level)
        //     {
        //         string audioPath = "file:///" + level.customSongInfo.path + "/" + level.customSongInfo.GetAudioPath();
        //         level.SetAudioClip(TemporaryAudioClip);
        //         level.beatmapLevelData.SetField("_audioClip", TemporaryAudioClip);
        //        Resources.UnloadUnusedAssets();
        //        
        //    }
        internal void LoadAudio(string audioPath, CustomLevel customLevel, Action callback)
        {
            AudioClip audioClip;
            if (!LoadedAudioClips.ContainsKey(audioPath))
            {
                using (var www = new WWW(EncodePath(audioPath)))
                {
                    customLevel.AudioClipLoading = true;

                    audioClip = www.GetAudioClip(true, true, AudioType.UNKNOWN);

                    var timeout = Time.realtimeSinceStartup + 5;
                    while (audioClip.length == 0)
                    {
                        if (Time.realtimeSinceStartup > timeout)
                        {
                            Log("Audio clip: " + audioClip.name + " timed out...", LogSeverity.Notice);
                            break;
                        }

                    }

                    LoadedAudioClips.Add(audioPath, audioClip);
                }
            }
            else
            {
                audioClip = LoadedAudioClips[audioPath];
            }

            customLevel.SetAudioClip(audioClip);
            customLevel.InitData();
            if (callback != null)
                callback.Invoke();
            customLevel.AudioClipLoading = false;
            //      return null;
        }

        public void RetrieveNewSong(string songFolderName)
        {
            Log("Retrieving Single Song");
            Log("Folder: " + songFolderName);
            if (string.IsNullOrEmpty(songFolderName))
            {
                Log("Error Retrieving Song: No Folder Provided");
                return;
            }
            string newSongPath = Environment.CurrentDirectory + "/CustomSongs/" + songFolderName;
            Log("Path: " + newSongPath);
            var results = Directory.GetFiles(newSongPath, "info.json", SearchOption.AllDirectories);
            if (results.Length == 0)
            {
                Log("Custom song folder '" + newSongPath + "' is missing info.json files!", LogSeverity.Notice);
            }
            foreach (var result in results)
            {
                try
                {
                    var songPath = Path.GetDirectoryName(result).Replace('\\', '/');

                    var customSongInfo = GetCustomSongInfo(songPath);

                    if (customSongInfo == null) continue;



                    Log("Loaded new song.");
                    var level = LoadSong(customSongInfo);




                    CustomLevels.Add(level);
                    var orderedList = CustomLevels.OrderBy(x => x.songName);
                    CustomLevels = orderedList.ToList();


                    CustomLevelCollectionSO.AddCustomLevel(level);


                    ReloadHashes();

                    AreSongsLoaded = true;
                    AreSongsLoading = false;
                    LoadingProgress = 1;

                    _loadingTask = null;

                    SongsLoadedEvent?.Invoke(this, CustomLevels);


                }
                catch (Exception e)
                {
                    Log("Failed to load song folder: " + result, LogSeverity.Warning);
                    Log(e.ToString(), LogSeverity.Warning);
                }
            }
        }
        private void RetrieveAllSongs(bool fullRefresh)
        {
            var stopwatch = new Stopwatch();
            var levelList = new List<CustomLevel>();

            if (fullRefresh)
            {
                _customLevelPool.ReturnAll();
                _beatmapDataPool.ReturnAll();
                CustomLevels.Clear();
            }

            Action job = delegate
            {
                try
                {
                    stopwatch.Start();
                    var path = Environment.CurrentDirectory;
                    path = path.Replace('\\', '/');

                    var currentHashes = new List<string>();
                    var cachedHashes = new List<string>();
                    var cachedSongs = new string[0];

                    if (Directory.Exists(path + "/CustomSongs/.cache"))
                    {
                        cachedSongs = Directory.GetDirectories(path + "/CustomSongs/.cache");
                    }
                    else
                    {
                        Directory.CreateDirectory(path + "/CustomSongs/.cache");
                    }

                    if (!Directory.Exists(path + "/WIP Songs"))
                    {
                        Directory.CreateDirectory(path + "/WIP Songs");
                    }


                    var songFolders = Directory.GetDirectories(path + "/CustomSongs").ToList();
                    var WIPFolders = Directory.GetDirectories(path + "/WIP Songs").ToList();
                    var songCaches = Directory.GetDirectories(path + "/CustomSongs/.cache");


                    var loadedIDs = new List<string>();
                    var songZips = Directory.GetFiles(path + "/CustomSongs")
                    .Where(x =>
                    x.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                    || x.EndsWith(".beat", StringComparison.OrdinalIgnoreCase)
                    || x.EndsWith(".bmap", StringComparison.OrdinalIgnoreCase)).ToArray();



                    for (int i3 = 0; i3 < songZips.Length; i3++)
                    {
                        string songZip = songZips[i3];
                        //Check cache if zip already is extracted
                        string hash;
                        string trimmedZip = songZip;
                        trimmedZip = Utils.TrimEnd(trimmedZip, ".zip");
                        trimmedZip = Utils.TrimEnd(trimmedZip, ".beat");
                        trimmedZip = Utils.TrimEnd(trimmedZip, ".bmap");
                        if (Utils.CreateMD5FromFile(songZip, out hash))
                        {

                            using (var unzip = new Unzip(songZip))
                            {
                                try
                                {
                                    if (Directory.Exists(trimmedZip))
                                    {
                                        Log("Directory for Zip already exists, Extracting Zip to Cache instead.");
                                        cachedHashes.Add(hash);
                                        if (cachedSongs.Any(x => x.Contains(hash))) continue;
                                        unzip.ExtractToDirectory(path + "/CustomSongs/.cache/" + hash);

                                    }
                                    else
                                    {
                                        unzip.ExtractToDirectory(path + "/CustomSongs/" + trimmedZip.Replace(path + "/CustomSongs\\", ""));
                                        //Add hash if successfully extracted
                                        currentHashes.Add(hash);
                                    }

                                }
                                catch (Exception e)
                                {
                                    Log("Error extracting zip " + songZip + "\n" + e, LogSeverity.Warning);
                                }
                            }
                        }
                        else
                        {
                            Log("Error reading zip " + songZip, LogSeverity.Warning);
                        }
                    }

                    for (int i4 = 0; i4 < songZips.Length; i4++)
                    {
                        //Delete zip if successfully extracted
                        string hash;
                        if (Utils.CreateMD5FromFile(songZips[i4], out hash))
                        {
                            if (currentHashes.Contains(hash))
                            {
                                Log("Zip Successfully Extracted, deleting zip.");
                                File.SetAttributes(songZips[i4], FileAttributes.Normal);
                                File.Delete(songZips[i4]);
                            }
                        }
                    }

                    for (int i5 = 0; i5 < songCaches.Length; i5++)
                    {
                        var hash = Path.GetFileName(songCaches[i5]);
                        if (!cachedHashes.Contains(hash))
                        {
                            //Old cache
                            Directory.Delete(songCaches[i5], true);
                        }
                    }

                    float i = 0;
                    for (int i1 = 0; i1 < songFolders.Count; i1++)
                    {
                        i++;
                        var results = Directory.GetFiles(songFolders[i1], "info.json", SearchOption.AllDirectories);
                        if (results.Length == 0)
                        {
                            Log("Custom song folder '" + songFolders[i1] + "' is missing info.json files!", LogSeverity.Notice);
                            continue;
                        }


                        for (int i7 = 0; i7 < results.Length; i7++)
                        {
                            try
                            {
                                var songPath = Path.GetDirectoryName(results[i7]).Replace('\\', '/');
                                if (!fullRefresh)
                                {
                                    var c = CustomLevels.FirstOrDefault(x => x.customSongInfo.path == songPath);
                                    if(c)
                                    {
                                        loadedIDs.Add(c.levelID);
                                        continue;
                                    }
                                }

                                var customSongInfo = GetCustomSongInfo(songPath);

                                if (customSongInfo == null) continue;
                                var id = customSongInfo.GetIdentifier();
                                if (loadedIDs.Any(x => x == id))
                                {
                                    Log("Duplicate song found at " + customSongInfo.path, LogSeverity.Notice);
                                    continue;
                                }

                                loadedIDs.Add(id);

                                var count = i;
                                HMMainThreadDispatcher.instance.Enqueue(delegate
                                {
                                    if (_loadingCancelled) return;
                                    var level = LoadSong(customSongInfo);
                                    if (level != null)
                                    {

                                        levelList.Add(level);
                                    }

                                    LoadingProgress = count / songFolders.Count;
                                });
                            }
                            catch (Exception e)
                            {
                                Log("Failed to load song folder: " + results[i7], LogSeverity.Error);
                                Log(e.ToString(), LogSeverity.Error);
                            }
                        }
                    }
                    for (int i2 = 0; i2 < WIPFolders.Count; i2++)
                    {
                        i++;
                        var results = Directory.GetFiles(WIPFolders[i2], "info.json", SearchOption.AllDirectories);
                        if (results.Length == 0)
                        {
                            Log("Custom song folder '" + WIPFolders[i2] + "' is missing info.json files!", LogSeverity.Notice);
                            continue;
                        }


                        for (int i6 = 0; i6 < results.Length; i6++)
                        {
                            try
                            {
                                var songPath = Path.GetDirectoryName(results[i6]).Replace('\\', '/');
                                if (!fullRefresh)
                                {
                                    var c = CustomLevels.FirstOrDefault(x => x.customSongInfo.path == songPath);
                                    if (c)
                                    {
                                        loadedIDs.Add(c.levelID);
                                        continue;
                                    }
                                }

                                var customSongInfo = GetCustomSongInfo(songPath);

                                if (customSongInfo == null) continue;
                                var id = customSongInfo.GetIdentifier();
                                if (loadedIDs.Any(x => x == id))
                                {
                                    Log("Duplicate song found at " + customSongInfo.path, LogSeverity.Notice);
                                    continue;
                                }

                                loadedIDs.Add(id);



                                var count = i;
                                HMMainThreadDispatcher.instance.Enqueue(delegate
                                {
                                    if (_loadingCancelled) return;
                                    var level = LoadSong(customSongInfo);
                                    level.inWipFolder = true;
                                    if (level != null)
                                    {

                                        levelList.Add(level);
                                    }

                                    LoadingProgress = count / WIPFolders.Count;
                                });
                            }
                            catch (Exception e)
                            {
                                Log("Failed to load song folder: " + results[i6], LogSeverity.Warning);
                                Log(e.ToString(), LogSeverity.Warning);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log("RetrieveAllSongs failed:", LogSeverity.Error);
                    Log(e.ToString(), LogSeverity.Error);
                }

            };

            Action finish = delegate
            {
                stopwatch.Stop();
                Log("Loaded " + levelList.Count + " new songs in " + stopwatch.Elapsed.Seconds + " seconds");

                CustomLevels.AddRange(levelList);
                var orderedList = CustomLevels.OrderBy(x => x.songName);
                CustomLevels = orderedList.ToList();

                for (int i = 0; i < CustomLevels.Count; i++)
                {
                    if (!CustomLevels[i].inWipFolder)
                        CustomLevelCollectionSO.AddCustomLevel(CustomLevels[i]);
                    else
                        WIPCustomLevelCollectionSO.AddCustomLevel(CustomLevels[i]);
                }

                ReloadHashes();

                AreSongsLoaded = true;
                AreSongsLoading = false;
                LoadingProgress = 1;

                _loadingTask = null;

                SongsLoadedEvent?.Invoke(this, CustomLevels);

                SongCore.Collections.SaveExtraSongData();

            };

            _loadingTask = new HMTask(job, finish);
            _loadingTask.Run();
        }

        private void ReloadHashes()
        {
            var additionalContentModelSO = Resources.FindObjectsOfTypeAll<AdditionalContentModelSO>().FirstOrDefault();
            HashSet<string> _alwaysOwnedBeatmapLevelIds = (HashSet<string>)additionalContentModelSO.GetField("_alwaysOwnedBeatmapLevelIds");
            HashSet<string> _alwaysOwnedBeatmapLevelPackIds = (HashSet<string>)additionalContentModelSO.GetField("_alwaysOwnedPacksIds");
            if (!_alwaysOwnedBeatmapLevelPackIds.Contains("ModdedCustomMaps"))
                _alwaysOwnedBeatmapLevelPackIds.Add("ModdedCustomMaps");
            if (!_alwaysOwnedBeatmapLevelPackIds.Contains("ModdedWIPMaps"))
                _alwaysOwnedBeatmapLevelPackIds.Add("ModdedWIPMaps");

            foreach (BeatmapLevelSO level in CustomLevelCollectionSO._levelList)
            {
                if (level is CustomLevel)
                    _alwaysOwnedBeatmapLevelIds.Add(level.levelID);
            }
            foreach (BeatmapLevelSO level in WIPCustomLevelCollectionSO._levelList)
            {
                if (level is CustomLevel)
                    _alwaysOwnedBeatmapLevelIds.Add(level.levelID);
            }

            additionalContentModelSO.SetPrivateField("_alwaysOwnedBeatmapLevelIds", _alwaysOwnedBeatmapLevelIds);
            additionalContentModelSO.SetPrivateField("_alwaysOwnedPacksIds", _alwaysOwnedBeatmapLevelPackIds);
            //  Console.WriteLine("1");
            BeatmapLevelsModelSO beatmapLevelsModelSO = Resources.FindObjectsOfTypeAll<BeatmapLevelsModelSO>().FirstOrDefault();
            HMCache<string, IBeatmapLevel> _loadedBeatmapLevels = beatmapLevelsModelSO.GetField<HMCache<string, IBeatmapLevel>>("_loadedBeatmapLevels");
            Dictionary<string, IPreviewBeatmapLevel> _loadedPreviewBeatmapLevels = beatmapLevelsModelSO.GetField<Dictionary<string, IPreviewBeatmapLevel>>("_loadedPreviewBeatmapLevels");
            //  Console.WriteLine("2");
            foreach (var packs in CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks)
            {
                //       Console.WriteLine("3.1  " + packs?.packName);
                if (packs == null)
                {
                    Console.WriteLine("Null Pack, Removing");
                    CustomBeatmapLevelPackCollectionSO._customBeatmapLevelPacks.Remove(packs as BeatmapLevelPackSO);
                }
                foreach (var level in packs?.beatmapLevelCollection?.beatmapLevels)
                {
                    //                  Console.WriteLine("3.2");
                    if (level != null)
                        if (!_loadedPreviewBeatmapLevels.ContainsKey(level.levelID)) { _loadedPreviewBeatmapLevels.Add(level.levelID, level); }
                    if (level is IBeatmapLevel)
                    {
                        if (_loadedBeatmapLevels.GetFromCache(level.levelID) == null)
                        {
                            _loadedBeatmapLevels.PutToCache(level.levelID, (IBeatmapLevel)level);
                        }
                    }
                }
            }
            //      Console.WriteLine("4");
            beatmapLevelsModelSO.SetField("_loadedBeatmapLevels", _loadedBeatmapLevels);
            beatmapLevelsModelSO.SetField("_loadedPreviewBeatmapLevels", _loadedPreviewBeatmapLevels);
            beatmapLevelsModelSO.SetField("_loadedBeatmapLevelPackCollection", CustomBeatmapLevelPackCollectionSO);
            beatmapLevelsModelSO.SetField("_allLoadedBeatmapLevelPackCollection", CustomBeatmapLevelPackCollectionSO);

        }

        private CustomLevel LoadSong(CustomSongInfo song)
        {
            try
            {
                var newLevel = _customLevelPool.Get();
                newLevel.Init(song);
                song.difficultyLevels = song.difficultyLevels.ToList().OrderBy(x => x.difficultyRank).ToList().ToArray();
                newLevel.SetAudioClip(TemporaryAudioClip);

                var difficultyBeatmaps = new List<BeatmapLevelSO.DifficultyBeatmap>();
                for (int i = 0; i < song.difficultyLevels.Length; i++)
                {
                    CustomSongInfo.DifficultyLevel diffBeatmap = song.difficultyLevels[i];
                    try
                    {
                        var difficulty = diffBeatmap.difficulty.ToEnum(BeatmapDifficulty.Normal);

                        if (string.IsNullOrEmpty(diffBeatmap.json))
                        {
                            Log("Couldn't find or parse difficulty json " + song.path + "/" + diffBeatmap.jsonPath, LogSeverity.Notice);
                            continue;
                        }
                        if(newLevel.customSongInfo.oneSaber)
                            diffBeatmap.characteristic = SongLoader.oneSaberCharacteristicName;
                        else
                        switch (diffBeatmap.characteristic)
                        {
                            case "Standard":
                                diffBeatmap.characteristic = SongLoader.standardCharacteristicName;
                                break;
                            case "One Saber":
                                diffBeatmap.characteristic = SongLoader.oneSaberCharacteristicName;
                                break;
                            case "No Arrows":
                                diffBeatmap.characteristic = SongLoader.noArrowsCharacteristicName;
                                break;
                        }
                        var newBeatmapData = _beatmapDataPool.Get();
                        newBeatmapData.SetJsonData(diffBeatmap.json);

                        var newDiffBeatmap = new CustomLevel.CustomDifficultyBeatmap(newLevel, difficulty,
                            diffBeatmap.difficultyRank, diffBeatmap.noteJumpMovementSpeed, diffBeatmap.noteJumpStartBeatOffset, newBeatmapData, diffBeatmap.characteristic);
                        difficultyBeatmaps.Add(newDiffBeatmap);
                    }
                    catch (Exception e)
                    {
                        Log("Error parsing difficulty level in song: " + song.path, LogSeverity.Warning);
                        Log(e.Message, LogSeverity.Warning);
                    }
                }

                if (difficultyBeatmaps.Count == 0) return null;

                newLevel.SetDifficultyBeatmaps(difficultyBeatmaps.ToArray(), beatmapCharacteristicSOCollection, newLevel.customSongInfo.oneSaber);
                newLevel.InitData();
                //LoadSprite(song.path + "/" + song.coverImagePath, newLevel);

                return newLevel;
            }
            catch (Exception e)
            {
                Log("Failed to load song: " + song.path, LogSeverity.Warning);
                Log(e.ToString(), LogSeverity.Warning);
            }

            return null;
        }

        private CustomSongInfo GetCustomSongInfo(string songPath)
        {
            var infoText = File.ReadAllText(songPath + "/info.json");
            CustomSongInfo songInfo;
            try
            {
                songInfo = JsonConvert.DeserializeObject<CustomSongInfo>(infoText);
                for (int i = 0; i < songInfo.difficultyLevels.Length; i++)
                {
                    songInfo.difficultyLevels[i].difficultyRank = (int)Utils.ToEnum(songInfo.difficultyLevels[i].difficulty, BeatmapDifficulty.Normal);
                }
            }
            catch (Exception)
            {
                Log("Error parsing song: " + songPath, LogSeverity.Warning);
                return null;
            }

            songInfo.path = songPath;
            return songInfo;
        }

        private void Log(string message, IPA.Logging.Logger.Level severity = IPA.Logging.Logger.Level.Info)
        {
            Plugin.logger.Log(severity, message);
        }


        internal static void GetIcons()
        {
            if (!CustomSongsIcon)
                CustomSongsIcon = Utils.LoadSpriteFromResources("SongLoaderPlugin.Icons.CustomSongs.png");
            if (!MissingCharIcon)
                MissingCharIcon = Utils.LoadSpriteFromResources("SongLoaderPlugin.Icons.MissingChar.png");
            if (!LightshowIcon)
                LightshowIcon = Utils.LoadSpriteFromResources("SongLoaderPlugin.Icons.Lightshow.png");
            if (!ExtraDiffsIcon)
                ExtraDiffsIcon = Utils.LoadSpriteFromResources("SongLoaderPlugin.Icons.ExtraDiffsIcon.png");

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (CurrentLevelPlaying != null)
                {
                    ReloadCurrentSong();
                    return;
                }
                RefreshSongs(Input.GetKey(KeyCode.LeftControl));
            }
        }

        private void ReloadCurrentSong()
        {
            GameplayCoreSceneSetupData data = BS_Utils.Plugin.LevelData?.GameplayCoreSceneSetupData;
            Console.WriteLine("Is Set? : " + BS_Utils.Plugin.LevelData.IsSet);
            if (!BS_Utils.Plugin.LevelData.IsSet) return;

            if (!data.gameplayModifiers.noFail) return;
            var reloadedLevel = LoadSong(GetCustomSongInfo(CurrentLevelPlaying.customLevel.customSongInfo.path));
            if (reloadedLevel == null) return;

            reloadedLevel.FixBPMAndGetNoteJumpMovementSpeed();
            reloadedLevel.SetAudioClip(CurrentLevelPlaying.customLevel.previewAudioClip);

            RemoveSong(CurrentLevelPlaying.customLevel);

            SongCore.Collections.AddSong(reloadedLevel.levelID, reloadedLevel.customSongInfo.path, true);

            CustomLevels.Add(reloadedLevel);

            ReloadHashes();

            CustomLevelCollectionSO.AddCustomLevel(reloadedLevel);

            var orderedList = CustomLevels.OrderBy(x => x.songName);
            CustomLevels = orderedList.ToList();

            var restartController = Resources.FindObjectsOfTypeAll<StandardLevelRestartController>().FirstOrDefault();
            if (restartController == null)
            {
                Console.WriteLine("No restart controller!");
                return;
            }

            restartController.RestartLevel();
        }

        private static string EncodePath(string path)
        {
            path = Uri.EscapeDataString(path);
            path = path.Replace("%2F", "/"); //Forward slash gets encoded, but it shouldn't.
            path = path.Replace("%3A", ":"); //Same with semicolon.
            return path;
        }









        public static void RegisterCapability(string capability)
        {
            SongCore.Collections.RegisterCapability(capability);
        }

        public static BeatmapCharacteristicSO RegisterCustomCharacteristic(Sprite Icon, string CharacteristicName, string HintText, string SerializedName, string CompoundIdPartName)
        {
            return SongCore.Collections.RegisterCustomCharacteristic(Icon, CharacteristicName, HintText, SerializedName, CompoundIdPartName);
        }



        public static void DeregisterizeCapability(string capability)
        {
            SongCore.Collections.DeregisterizeCapability(capability);
        }


    }
}