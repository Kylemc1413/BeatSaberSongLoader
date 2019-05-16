using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SongLoaderPlugin.OverrideClasses
{
    public class CustomLevel : BeatmapLevelSO, IScriptableObjectResetable
    {
        public CustomSongInfo customSongInfo { get; private set; }
        public bool AudioClipLoading { get; set; }
        public bool BPMAndNoteSpeedFixed { get; private set; }
        public bool inWipFolder = false;
        public void Init(CustomSongInfo newCustomSongInfo)
        {
            customSongInfo = newCustomSongInfo;
            _levelID = customSongInfo.GetIdentifier();
            _songName = customSongInfo.songName;
            _songSubName = customSongInfo.songSubName;
            _songAuthorName = customSongInfo.GetSongAuthor();
            _beatsPerMinute = customSongInfo.beatsPerMinute;
            _songTimeOffset = customSongInfo.songTimeOffset;
            _shuffle = customSongInfo.shuffle;
            _shufflePeriod = customSongInfo.shufflePeriod;
            _previewStartTime = customSongInfo.previewStartTime;
            _previewDuration = customSongInfo.previewDuration;
            _environmentSceneInfo = EnvironmentsLoader.GetSceneInfo(customSongInfo.environmentName);
            _coverImageTexture2D = SongLoader.CustomSongsIcon.texture;
            string _customEnvironment = customSongInfo.customEnvironment;
            string _customEnvironmentHash = customSongInfo.customEnvironmentHash;

        }

        public void SetAudioClip(AudioClip newAudioClip)
        {
            _audioClip = newAudioClip;
        }

        public void SetCoverImage(Sprite newCoverImage)
        {
            _coverImageTexture2D = newCoverImage.texture;
        }

        public void SetDifficultyBeatmaps(DifficultyBeatmap[] newDifficultyBeatmaps, BeatmapCharacteristicSO[] characteristicsSO, bool singleSaber = false)
        { 
                List<DifficultyBeatmapSet> beatmapsets = new List<DifficultyBeatmapSet>();
                List<DifficultyBeatmap> missingCharacteristicBeatmaps = new List<DifficultyBeatmap>();
                //Check Standard Characteristics
                for (int i = 0; i < characteristicsSO.Length; i++)
                {
                    List<DifficultyBeatmap> beatmaps = new List<DifficultyBeatmap>();
                    foreach (DifficultyBeatmap beatmap in newDifficultyBeatmaps)
                    {
                        string characteristic = (beatmap as CustomDifficultyBeatmap).Characteristic;
                  

                    if (characteristic == characteristicsSO[i].characteristicName || (string.IsNullOrWhiteSpace(characteristic) && i == 0))
                            beatmaps.Add(beatmap);

                    }
                    if (beatmaps.Count > 0)
                        beatmapsets.Add(new DifficultyBeatmapSet(characteristicsSO[i], beatmaps.ToArray()));
                }
                //Check Custom Charactersistics
                for (int i = 0; i < SongCore.Collections.customCharacteristics.Count; i++)
                {
                    List<DifficultyBeatmap> beatmaps = new List<DifficultyBeatmap>();
                    foreach (DifficultyBeatmap beatmap in newDifficultyBeatmaps)
                    {
                        string characteristic = (beatmap as CustomDifficultyBeatmap).Characteristic;
                        if (characteristic == SongCore.Collections.customCharacteristics[i].characteristicName)
                            beatmaps.Add(beatmap);
                        else if (!SongCore.Collections.customCharacteristics.Any(x => x.characteristicName == characteristic)
                            && (characteristic != SongLoader.standardCharacteristicName && characteristic != SongLoader.oneSaberCharacteristicName && characteristic != SongLoader.noArrowsCharacteristicName && !string.IsNullOrWhiteSpace(characteristic)) )
                            missingCharacteristicBeatmaps.Add(beatmap);

                    }
                    if (beatmaps.Count > 0)
                        beatmapsets.Add(new DifficultyBeatmapSet(SongCore.Collections.customCharacteristics[i], beatmaps.ToArray()));
                }
                if (missingCharacteristicBeatmaps.Count > 0)
                    beatmapsets.Add(new DifficultyBeatmapSet
                        (SongCore.Collections.customCharacteristics.First(x => x.characteristicName == "Missing Characteristic"), missingCharacteristicBeatmaps.ToArray()));




                _difficultyBeatmapSets = beatmapsets.ToArray();
            

        }

        public void SetBeatmapCharacteristics(BeatmapCharacteristicSO[] newBeatmapCharacteristics)
        {
            _beatmapCharacteristics = newBeatmapCharacteristics;
        }

        public void FixBPMAndGetNoteJumpMovementSpeed()
        {
            //      Console.WriteLine("FixBPMOr NoteJump");
            if (BPMAndNoteSpeedFixed) return;
            var bpms = new Dictionary<float, int> { { _beatsPerMinute, 0 } };
            foreach (var diffLevel in customSongInfo.difficultyLevels)
            {
                if (string.IsNullOrEmpty(diffLevel.json)) continue;

                float? bpm, noteSpeed;
                int? noteJumpStartBeatOffset;
                IDifficultyBeatmap diffBeatmap = null;
                bool missingChar = false;

                string characteristic = diffLevel.characteristic;
                if (string.IsNullOrEmpty(characteristic))
                    characteristic = SongLoader.standardCharacteristicName;

                else if (characteristic != SongLoader.standardCharacteristicName && characteristic != SongLoader.oneSaberCharacteristicName && characteristic != SongLoader.noArrowsCharacteristicName)
                    missingChar = !(SongCore.Collections.customCharacteristics.Any(x => x.characteristicName == characteristic));
                switch (characteristic)
                {
                    case "Standard":
                        characteristic = SongLoader.standardCharacteristicName;
                        break;
                    case "One Saber":
                        characteristic = SongLoader.oneSaberCharacteristicName;
                        break;
                    case "No Arrows":
                        characteristic = SongLoader.noArrowsCharacteristicName;
                        break;
                }
                if (missingChar)
                    characteristic = "Missing Characteristic";
                foreach (DifficultyBeatmapSet set in _difficultyBeatmapSets)
                {

                    if (set.beatmapCharacteristic.characteristicName == characteristic)
                    {
                        diffBeatmap = set.difficultyBeatmaps.FirstOrDefault(x =>
                               diffLevel.difficulty.ToEnum(BeatmapDifficulty.Normal) == x.difficulty);
                    }

                }

                var customBeatmap = diffBeatmap as CustomDifficultyBeatmap;
                if (customBeatmap == null) continue;


                customBeatmap.ParseDiffJson(diffLevel.json, out bpm, out noteSpeed, out noteJumpStartBeatOffset);
                if (bpm.HasValue)
                {
                    if (bpms.ContainsKey(bpm.Value))
                    {
                        bpms[bpm.Value]++;
                    }
                    else
                    {
                        bpms.Add(bpm.Value, 1);
                    }
                }


                if (!noteSpeed.HasValue) return;
                customBeatmap.SetNoteJumpMovementSpeed(noteSpeed.Value);
                if (noteJumpStartBeatOffset.HasValue)
                    customBeatmap.SetNoteJumpStartBeatOffset(noteJumpStartBeatOffset.Value);




            }

            _beatsPerMinute = bpms.OrderByDescending(x => x.Value).First().Key;
            try
            {
                foreach (var beatmapset in _difficultyBeatmapSets)
                    foreach (var difficultyBeatmap in beatmapset.difficultyBeatmaps)
                    {
                        var customBeatmap = difficultyBeatmap as CustomDifficultyBeatmap;
                        if (customBeatmap == null) continue;
                        customBeatmap.BeatmapDataSO.SetRequiredDataForLoad(_beatsPerMinute, _shuffle, _shufflePeriod);
                        customBeatmap.BeatmapDataSO.Load();
                    }

                BPMAndNoteSpeedFixed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        public class CustomDifficultyBeatmap : DifficultyBeatmap
        {
            public string Characteristic { get; private set; }
      


            public CustomDifficultyBeatmap(IBeatmapLevel parentLevel, BeatmapDifficulty difficulty, int difficultyRank, float noteJumpMovementSpeed, int noteJumpStartBeatOffset, BeatmapDataSO beatmapData, string characteristic = "") : base(parentLevel, difficulty, difficultyRank, noteJumpMovementSpeed, noteJumpStartBeatOffset, beatmapData)
            {
                Characteristic = characteristic;
            }

            public CustomLevel customLevel
            {
                get { return level as CustomLevel; }
            }

            public CustomBeatmapDataSO BeatmapDataSO
            {
                get { return _beatmapData as CustomBeatmapDataSO; }
            }

            public void SetNoteJumpMovementSpeed(float newNoteJumpMovementSpeed)
            {
                _noteJumpMovementSpeed = newNoteJumpMovementSpeed;
            }
            public void SetNoteJumpStartBeatOffset(int newNoteJumpStartBeatOffset)
            {
                _noteJumpStartBeatOffset = newNoteJumpStartBeatOffset;
            }


            //This is quicker than using a JSON parser
            internal void ParseDiffJson(string json, out float? bpm, out float? noteJumpSpeed, out int? noteJumpStartBeatOffset)
            {
                bpm = null;
                noteJumpSpeed = null;
                noteJumpStartBeatOffset = null;
                var split = json.Split(':');
                for (var i = 0; i < split.Length; i++)
                {
                    try
                    {
                        //BPM and NoteJump
                        if (split[i].Contains("_beatsPerMinute"))
                        {
                            bpm = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                        }

                        if (split[i].Contains("_noteJumpSpeed"))
                        {
                            noteJumpSpeed = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                        }
                        if (split[i].Contains("_noteJumpStartBeatOffset"))
                        {
                            noteJumpStartBeatOffset = (int)Convert.ToDouble(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }





                }
            }

        
        }

        public void Reset()
        {
            _audioClip = null;
            BPMAndNoteSpeedFixed = false;
        }

     
    }
}
