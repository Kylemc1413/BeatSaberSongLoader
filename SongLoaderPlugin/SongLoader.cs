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
using CustomUI.BeatSaber;
using UnityEngine.UI;
using TMPro;
namespace SongLoaderPlugin
{
	public class SongLoader : MonoBehaviour
	{
        private static List<string> _capabilities = new List<string>();
        public static System.Collections.ObjectModel.ReadOnlyCollection<string> capabilities
        {
            get { return _capabilities.AsReadOnly(); }
        }


        public static UnityEngine.UI.Button infoButton;
        internal static CustomUI.BeatSaber.CustomMenu reqDialog;
        internal static CustomUI.BeatSaber.CustomListViewController reqViewController;
        internal static Sprite HaveReqIcon;
        internal static Sprite MissingReqIcon;
        internal static Sprite HaveSuggestionIcon;
        internal static Sprite MissingSuggestionIcon;
        internal static Sprite WarningIcon;
        internal static Sprite InfoIcon;




        public static event Action<SongLoader> LoadingStartedEvent;
		public static event Action<SongLoader, List<CustomLevel>> SongsLoadedEvent;
		public static List<CustomLevel> CustomLevels = new List<CustomLevel>();
		public static bool AreSongsLoaded { get; private set; }
		public static bool AreSongsLoading { get; private set; }
		public static float LoadingProgress { get; private set; }
		public static CustomLevelCollectionSO CustomLevelCollectionSO { get; private set; }
        private bool CustomPlatformsPresent = IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "Custom Platforms" && x.Version != "2.5.0");
		private int _currentPlatform = -1;

		public const string MenuSceneName = "Menu";
		public const string GameSceneName = "GameCore";
		
		private static readonly Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
		private static readonly Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();
		
		private LeaderboardScoreUploader _leaderboardScoreUploader;
		private StandardLevelDetailViewController _standardLevelDetailViewController;
		private StandardLevelSceneSetupDataSO _standardLevelSceneSetupData;
		private BeatmapCharacteristicSelectionViewController _characteristicViewController;
		private LevelListViewController _LevelListViewController;


		private readonly ScriptableObjectPool<CustomLevel> _customLevelPool = new ScriptableObjectPool<CustomLevel>();
		private readonly ScriptableObjectPool<CustomBeatmapDataSO> _beatmapDataPool = new ScriptableObjectPool<CustomBeatmapDataSO>();

		private ProgressBar _progressBar;

		private HMTask _loadingTask;
		private bool _loadingCancelled;
		private SceneEvents _sceneEvents;

		public static CustomLevel.CustomDifficultyBeatmap CurrentLevelPlaying { get; private set; }
        public static System.Collections.ObjectModel.ReadOnlyCollection<string> currentRequirements;
        public static System.Collections.ObjectModel.ReadOnlyCollection<string> currentSuggestions;

        public static readonly AudioClip TemporaryAudioClip = AudioClip.Create("temp", 1, 2, 1000, true);

		private LogSeverity _minLogSeverity;
		internal static bool _noArrowsSelected;
		private bool customSongColors;
		private bool customSongPlatforms;
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
			GameObject.Destroy(GameObject.Find("SongLoader Color Setter"));
			customSongColors = IllusionPlugin.ModPrefs.GetBool("Songloader", "customSongColors", true, true);
			customSongPlatforms = IllusionPlugin.ModPrefs.GetBool("Songloader", "customSongPlatforms", true, true);
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
				CurrentLevelPlaying = null;

				if (CustomLevelCollectionSO == null)
				{
					var levelCollectionSO = Resources.FindObjectsOfTypeAll<LevelCollectionSO>().FirstOrDefault();
					CustomLevelCollectionSO = CustomLevelCollectionSO.ReplaceOriginal(levelCollectionSO);
				}
				else
				{
					CustomLevelCollectionSO.ReplaceReferences();
				}
				if(_standardLevelDetailViewController == null)
				{
				_standardLevelDetailViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().FirstOrDefault();
				if (_standardLevelDetailViewController == null) return;
				_standardLevelDetailViewController.didPressPlayButtonEvent += StandardLevelDetailControllerOnDidPressPlayButtonEvent;
				}

				if (_LevelListViewController == null)
				{
					_LevelListViewController = Resources.FindObjectsOfTypeAll<LevelListViewController>().FirstOrDefault();
					if (_LevelListViewController == null) return;

					_LevelListViewController.didSelectLevelEvent += StandardLevelListViewControllerOnDidSelectLevelEvent;
				}



				if (_characteristicViewController == null)
				{
					_characteristicViewController = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSelectionViewController>().FirstOrDefault();
					if (_characteristicViewController == null) return;

					_characteristicViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristicEvent;
				}

                if (CustomPlatformsPresent)
                   CheckForPreviousPlatform();

            }
			else if (activeScene.name == GameSceneName)
			{
				_standardLevelSceneSetupData = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
				if (_standardLevelSceneSetupData == null) return;
				var level = _standardLevelSceneSetupData.difficultyBeatmap;
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

                //Set environment if the song has customEnvironment
                if(CustomPlatformsPresent)
                CheckCustomSongEnvironment(song);
				//Set enviroment colors for the song if it has song specific colors
				if(customSongColors)
				song.SetSongColors(CurrentLevelPlaying.colorLeft, CurrentLevelPlaying.colorRight, CurrentLevelPlaying.hasCustomColors);
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

					beatmapObjectSpawnController.Init(CurrentLevelPlaying.level.beatsPerMinute,
						CurrentLevelPlaying.beatmapData.beatmapLinesData.Length,
						CurrentLevelPlaying.noteJumpMovementSpeed, CurrentLevelPlaying.noteJumpStartBeatOffset, disappearingArrows);
				}
			}

			//Also change beatmap to no arrow if no arrow was selected, since Beat Saber no longer does runtime conversion for that.

			if (!_noArrowsSelected) return;
			var gameplayCore = Resources.FindObjectsOfTypeAll<GameplayCoreSceneSetup>().FirstOrDefault();
			if (gameplayCore == null) return;
			Console.WriteLine("Applying no arrow transformation");
			var transformedBeatmap = BeatmapDataNoArrowsTransform.CreateTransformedData(CurrentLevelPlaying.beatmapData);
			var beatmapDataModel = gameplayCore.GetPrivateField<BeatmapDataModel>("_beatmapDataModel");
			beatmapDataModel.SetPrivateField("_beatmapData", transformedBeatmap);
		}

        private void CheckForPreviousPlatform()
        {
            if (_currentPlatform != -1)
            {
                CustomFloorPlugin.PlatformManager.Instance.ChangeToPlatform(_currentPlatform);
            }
        }
        private void CheckCustomSongEnvironment(CustomLevel song)
        {
            if (song.customSongInfo.customEnvironment != null)
            {
                int _customPlatform = customEnvironment(song);
                if (_customPlatform != -1)
                {
                    _currentPlatform = CustomFloorPlugin.PlatformManager.Instance.currentPlatformIndex;
                    if (customSongPlatforms && _customPlatform != _currentPlatform)
                    {
                        CustomFloorPlugin.PlatformManager.Instance.ChangeToPlatform(_customPlatform, false);
                    }
                }
            }
        }

		private void StandardLevelListViewControllerOnDidSelectLevelEvent(LevelListViewController levelListViewController, IBeatmapLevel level)
		{
			var customLevel = level as CustomLevel;
			if (customLevel == null) return;

			if (customLevel.audioClip != TemporaryAudioClip || customLevel.AudioClipLoading) return;

			var levels = levelListViewController.GetPrivateField<IBeatmapLevel[]>("_levels").ToList();
			
			Action callback = delegate
			{
				levelListViewController.SetPrivateField("_selectedLevel", null);
				levelListViewController.HandleLevelListTableViewDidSelectRow(null, levels.IndexOf(customLevel));
			};

			customLevel.FixBPMAndGetNoteJumpMovementSpeed();
			StartCoroutine(LoadAudio(
				"file:///" + customLevel.customSongInfo.path + "/" + customLevel.customSongInfo.GetAudioPath(), customLevel,
				callback));
		}

		private void OnDidSelectBeatmapCharacteristicEvent(BeatmapCharacteristicSelectionViewController viewController, BeatmapCharacteristicSO characteristic)
		{
			_noArrowsSelected = characteristic.characteristicName == "No Arrows";
		}

		public void LoadAudioClipForLevel(CustomLevel customLevel, Action<CustomLevel> clipReadyCallback)
		{
			Action callback = delegate { clipReadyCallback(customLevel); };
			
			customLevel.FixBPMAndGetNoteJumpMovementSpeed();
			StartCoroutine(LoadAudio(
				"file:///" + customLevel.customSongInfo.path + "/" + customLevel.customSongInfo.GetAudioPath(), customLevel,
				callback));
		}

		private IEnumerator WaitRemoveScores()
		{
			yield return new WaitForSecondsRealtime(1f);
			RemoveCustomScores();
		}

		private void StandardLevelDetailControllerOnDidPressPlayButtonEvent(StandardLevelDetailViewController songDetailViewController)
		{
			if (!NoteHitVolumeChanger.PrefabFound) return;
			var level = songDetailViewController.difficultyBeatmap.level;
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
					Log("Some plugin is throwing exception from the LoadingStartedEvent!", LogSeverity.Error);
					Log(e.ToString(), LogSeverity.Error);
				}
			}

			foreach (var customLevel in CustomLevels)
			{
				CustomLevelCollectionSO.RemoveLevel(customLevel);
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

			CustomLevelCollectionSO.RemoveLevel(customLevel);

			foreach (var difficultyBeatmap in customLevel.difficultyBeatmaps)
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

		private void LoadSprite(string spritePath, CustomLevel customLevel)
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
					Log("Failed to load cover image: " + spritePath);
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
		
		private IEnumerator LoadAudio(string audioPath, CustomLevel customLevel, Action callback)
		{
			AudioClip audioClip;
			if (!LoadedAudioClips.ContainsKey(audioPath))
			{
				using (var www = new WWW(EncodePath(audioPath)))
				{
					customLevel.AudioClipLoading = true;
					yield return www;
					
					audioClip = www.GetAudioClip(true, true, AudioType.UNKNOWN);

					var timeout = Time.realtimeSinceStartup + 5;
					while (audioClip.length == 0)
					{
						if (Time.realtimeSinceStartup > timeout)
						{
							Log("Audio clip: " + audioClip.name + " timed out...", LogSeverity.Warn);
							break;
						}

						yield return null;
					}
					
					LoadedAudioClips.Add(audioPath, audioClip);
				}
			}
			else
			{
				audioClip = LoadedAudioClips[audioPath];
			}

			customLevel.SetAudioClip(audioClip);
			callback.Invoke();
			customLevel.AudioClipLoading = false;
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


					var songZips = Directory.GetFiles(path + "/CustomSongs")
						.Where(x => x.ToLower().EndsWith(".zip") || x.ToLower().EndsWith(".beat") || x.ToLower().EndsWith(".bmap")).ToArray();
					foreach (var songZip in songZips)
					{
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
									Log("Error extracting zip " + songZip + "\n" + e, LogSeverity.Warn);
								}
							}
						}
						else
						{
							Log("Error reading zip " + songZip, LogSeverity.Warn);
						}
					}

			   
					var songFolders = Directory.GetDirectories(path + "/CustomSongs").ToList();
					var songCaches = Directory.GetDirectories(path + "/CustomSongs/.cache");

					foreach (var songZip in songZips)
					{
						//Delete zip if successfully extracted
						string hash;
						if (Utils.CreateMD5FromFile(songZip, out hash))
						{
							if (currentHashes.Contains(hash))
							{
								Log("Zip Successfully Extracted, deleting zip.");
								File.SetAttributes(songZip, FileAttributes.Normal);
								File.Delete(songZip);
							}
						}
					}

					foreach (var song in songCaches)
					{
						var hash = Path.GetFileName(song);
						if (!cachedHashes.Contains(hash))
						{
							//Old cache
							Directory.Delete(song, true);
						}
					}



					var loadedIDs = new List<string>();
					
					float i = 0;
					foreach (var song in songFolders)
					{
						i++;
						var results = Directory.GetFiles(song, "info.json", SearchOption.AllDirectories);
						if (results.Length == 0)
						{
							Log("Custom song folder '" + song + "' is missing info.json files!", LogSeverity.Warn);
							continue;
						}

						
						foreach (var result in results)
						{
							try
							{
								var songPath = Path.GetDirectoryName(result).Replace('\\', '/');
								if (!fullRefresh)
								{
									if (CustomLevels.Any(x => x.customSongInfo.path == songPath))
									{
										continue;
									}
								}

								var customSongInfo = GetCustomSongInfo(songPath);

								if (customSongInfo == null) continue;
								var id = customSongInfo.GetIdentifier();
								if (loadedIDs.Any(x => x == id))
								{
									Log("Duplicate song found at " + customSongInfo.path, LogSeverity.Warn);
									continue;
								}

								loadedIDs.Add(id);

								if (CustomPlatformsPresent && customSongPlatforms)
								{
									if (customSongInfo.customEnvironment != null)
									{
										if (findCustomEnvironment(customSongInfo.customEnvironment) == -1)
										{
											Console.WriteLine("CustomPlatform not found: " + customSongInfo.customEnvironment);
											if (customSongInfo.customEnvironmentHash != null)
											{
												Console.WriteLine("Downloading with hash: " + customSongInfo.customEnvironmentHash);
												StartCoroutine(downloadCustomPlatform(customSongInfo.customEnvironmentHash, customSongInfo.customEnvironment));
											}
										}
									}
								}

								var i1 = i;
								HMMainThreadDispatcher.instance.Enqueue(delegate
								{
									if (_loadingCancelled) return;
									var level = LoadSong(customSongInfo);
									if (level != null)
									{
										levelList.Add(level);
									}

									LoadingProgress = i1 / songFolders.Count;
								});
							}
							catch (Exception e)
							{
								Log("Failed to load song folder: " + result, LogSeverity.Warn);
								Log(e.ToString(), LogSeverity.Warn);
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

				foreach (var customLevel in CustomLevels)
				{	
					CustomLevelCollectionSO.AddCustomLevel(customLevel);
				}

				AreSongsLoaded = true;
				AreSongsLoading = false;
				LoadingProgress = 1;

				_loadingTask = null;
				
				if (SongsLoadedEvent != null)
				{
					SongsLoadedEvent(this, CustomLevels);
				}

			};
			
			_loadingTask = new HMTask(job, finish);
			_loadingTask.Run();
		}

		private CustomLevel LoadSong(CustomSongInfo song)
		{
			try
			{
				var newLevel = _customLevelPool.Get();
				newLevel.Init(song);
				newLevel.SetAudioClip(TemporaryAudioClip);

				var difficultyBeatmaps = new List<LevelSO.DifficultyBeatmap>();
				foreach (var diffBeatmap in song.difficultyLevels)
				{
					try
					{
						var difficulty = diffBeatmap.difficulty.ToEnum(BeatmapDifficulty.Normal);

						if (string.IsNullOrEmpty(diffBeatmap.json))
						{
							Log("Couldn't find or parse difficulty json " + song.path + "/" + diffBeatmap.jsonPath, LogSeverity.Warn);
							continue;
						}

						var newBeatmapData = _beatmapDataPool.Get();
						newBeatmapData.SetJsonData(diffBeatmap.json);

						var newDiffBeatmap = new CustomLevel.CustomDifficultyBeatmap(newLevel, difficulty,
							diffBeatmap.difficultyRank, diffBeatmap.noteJumpMovementSpeed, diffBeatmap.noteJumpStartBeatOffset, newBeatmapData);
						difficultyBeatmaps.Add(newDiffBeatmap);
					}
					catch (Exception e)
					{
						Log("Error parsing difficulty level in song: " + song.path, LogSeverity.Warn);
						Log(e.Message, LogSeverity.Warn);
					}
				}

				if (difficultyBeatmaps.Count == 0) return null;

				newLevel.SetDifficultyBeatmaps(difficultyBeatmaps.ToArray());
				newLevel.InitData();

				LoadSprite(song.path + "/" + song.coverImagePath, newLevel);
				return newLevel;
			}
			catch (Exception e)
			{
				Log("Failed to load song: " + song.path, LogSeverity.Warn);
				Log(e.ToString(), LogSeverity.Warn);
			}

			return null;
		}

		private CustomSongInfo GetCustomSongInfo(string songPath)
		{
			var infoText = File.ReadAllText(songPath + "/info.json");
			CustomSongInfo songInfo;
			try
			{
				songInfo = JsonUtility.FromJson<CustomSongInfo>(infoText);
			}
			catch (Exception)
			{
				Log("Error parsing song: " + songPath, LogSeverity.Warn);
				return null;
			}

			songInfo.path = songPath;
            if (songInfo.lighters == null)
                songInfo.lighters = new string[0];
            if (songInfo.mappers == null)
                songInfo.mappers = new string[0];

            //Here comes SimpleJSON to the rescue when JSONUtility can't handle an array.
            var diffLevels = new List<CustomSongInfo.DifficultyLevel>();
			var n = JSON.Parse(infoText);
			var diffs = n["difficultyLevels"];
			for (int i = 0; i < diffs.AsArray.Count; i++)
			{
				n = diffs[i];
				var difficulty = Utils.ToEnum(n["difficulty"], BeatmapDifficulty.Normal);
				var difficultyRank = (int)difficulty;
				
				diffLevels.Add(new CustomSongInfo.DifficultyLevel
				{
					difficulty = n["difficulty"],
					difficultyRank = difficultyRank,
					audioPath = n["audioPath"],
					jsonPath = n["jsonPath"],
					noteJumpMovementSpeed = n["noteJumpMovementSpeed"]
				});
			}

			songInfo.difficultyLevels = diffLevels.ToArray();
			return songInfo;
		}

		private void Log(string message, LogSeverity severity = LogSeverity.Info)
		{
			if (severity < _minLogSeverity) return;
			Console.WriteLine("Song Loader [" + severity.ToString().ToUpper() + "]: " + message);
		}

        private static void InitRequirementsMenu()
        {
            reqDialog = BeatSaberUI.CreateCustomMenu<CustomMenu>("Additional Song Information");
            reqViewController = BeatSaberUI.CreateViewController<CustomListViewController>();

            RectTransform confirmContainer = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
            confirmContainer.SetParent(reqViewController.rectTransform, false);
            confirmContainer.sizeDelta = new Vector2(60f, 0f);
            GetIcons();
            reqDialog.SetMainViewController(reqViewController, true);


        }

        internal static void showSongRequirements(CustomLevel.CustomDifficultyBeatmap beatmap, CustomSongInfo songInfo)
        {
            if (reqDialog == null)
                InitRequirementsMenu();
            //   suggestionsList.text = "";

            reqViewController.Data.Clear();
            if(songInfo?.mappers?.Length > 0)
            {
                foreach (string mapper in songInfo.mappers)
                {
                    reqViewController.Data.Add(new CustomCellInfo(mapper, "Mapping", InfoIcon));
                }
            }
            if (songInfo?.lighters?.Length > 0)
            {
                foreach (string lighter in songInfo.lighters)
                {
                    reqViewController.Data.Add(new CustomCellInfo(lighter, "Lighting", InfoIcon));
                }
            }
            if (beatmap.requirements.Count > 0)
            {
                foreach (string req in beatmap.requirements)
                {
                    //    Console.WriteLine(req);
                    if (!capabilities.Contains(req))
                        reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Missing Requirement", MissingReqIcon));
                    else
                        reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Requirement", HaveReqIcon));
                }
            }
            if (beatmap.warnings.Count > 0)
            {
                foreach (string req in beatmap.warnings)
                {

                    //    Console.WriteLine(req);

                    reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Warning", WarningIcon));
                }
            }
            if (beatmap.information.Count > 0)
            {
                foreach (string req in beatmap.information)
                {

                    //    Console.WriteLine(req);

                    reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Info", InfoIcon));
                }
            }
            if (beatmap.suggestions.Count > 0)
            {
                foreach (string req in beatmap.suggestions)
                {

                //    Console.WriteLine(req);
                    if (!capabilities.Contains(req))
                        reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Missing Suggestion", MissingSuggestionIcon));
                    else
                        reqViewController.Data.Add(new CustomCellInfo("<size=75%>" + req, "Suggestion", HaveSuggestionIcon));
                }
            }

            reqDialog.Present();
            reqViewController._customListTableView.ReloadData();
        }
                
        internal static void GetIcons()
        {
                MissingReqIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.RedX.png");
                HaveReqIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.GreenCheck.png");
                HaveSuggestionIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.YellowCheck.png");
                MissingSuggestionIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.YellowX.png");
                WarningIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.Warning.png");
                InfoIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.Info.png");
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
			if (!_standardLevelSceneSetupData.gameplayCoreSetupData.gameplayModifiers.noFail) return;
			var reloadedLevel = LoadSong(GetCustomSongInfo(CurrentLevelPlaying.customLevel.customSongInfo.path));
			if (reloadedLevel == null) return;
			
			reloadedLevel.FixBPMAndGetNoteJumpMovementSpeed();
			reloadedLevel.SetAudioClip(CurrentLevelPlaying.customLevel.audioClip);
					
			RemoveSong(CurrentLevelPlaying.customLevel);
			CustomLevels.Add(reloadedLevel);
			
			CustomLevelCollectionSO.AddCustomLevel(reloadedLevel);
			
			var orderedList = CustomLevels.OrderBy(x => x.songName);
			CustomLevels = orderedList.ToList();
			
			_standardLevelSceneSetupData.__WillBeUsedInTransition();
			_standardLevelSceneSetupData.Init(
				reloadedLevel.GetDifficultyBeatmap(_standardLevelSceneSetupData.difficultyBeatmap.difficulty),
				_standardLevelSceneSetupData.gameplayCoreSetupData);

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

		private int customEnvironment(CustomLevel song)
		{
			if(!CustomPlatformsPresent)
				return -1;
			return findCustomEnvironment(song.customSongInfo.customEnvironment);
		}

		private int findCustomEnvironment(string name) {
            
			CustomFloorPlugin.CustomPlatform[] _customPlatformsList = CustomFloorPlugin.PlatformManager.Instance.GetPlatforms();
			int platIndex = 0;
			foreach (CustomFloorPlugin.CustomPlatform plat in _customPlatformsList)
			{
				if (plat.platName == name)
					return platIndex;
				platIndex++;
			}
			Console.WriteLine(name + " not found!");
            
			return -1;
		}

		[Serializable]
		public class platformDownloadData
		{
			public string name;
			public string author;
			public string image;
			public string hash;
			public string download;
			public string date;
		}

		private IEnumerator downloadCustomPlatform(string hash, string name)
		{
			using (UnityWebRequest www = UnityWebRequest.Get("https://modelsaber.assistant.moe/api/v1/platform/get.php?filter=hash:" + hash))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError || www.isHttpError)
				{
					Console.WriteLine(www.error);
				}
				else
				{
					platformDownloadData downloadData = JsonUtility.FromJson<platformDownloadData>(JSON.Parse(www.downloadHandler.text)[0].ToString());
					if (downloadData.name == name)
					{
						StartCoroutine(_downloadCustomPlatform(downloadData));
					}
				}
			}
		}

		private IEnumerator _downloadCustomPlatform(platformDownloadData downloadData)
		{
			using (UnityWebRequest www = UnityWebRequest.Get(downloadData.download))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError || www.isHttpError)
				{
					Console.WriteLine(www.error);
				}
				else
				{
					string customPlatformsFolderPath = Path.Combine(Environment.CurrentDirectory, "CustomPlatforms", downloadData.name);
					System.IO.File.WriteAllBytes(@customPlatformsFolderPath + ".plat", www.downloadHandler.data);
				    //refreshPlatforms();
				}
			}
		}

        public static void RegisterCapability(string capability)
        {
            if(!_capabilities.Contains(capability))
            _capabilities.Add(capability);
        }

        public static void DeregisterizeCapability(string capability)
        {
                _capabilities.Remove(capability);
        }


    }
}